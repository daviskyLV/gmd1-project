using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// Used to compute a mesh's vertices, triangles, UVs and normals. Job size should be Vertices length. Mesh is assumed to be a square.
/// Works based on this principle: https://youtu.be/c2BUgXdjZkg?si=CJVV-fBdKOXxSmDF&t=164
/// </summary>
[BurstCompile]
public struct ChunkRendererJob : IJobParallelFor
{
    /// <summary>
    /// Heightmap data, where vertices per line follows this https://youtu.be/c2BUgXdjZkg?si=CJVV-fBdKOXxSmDF&t=164
    /// </summary>
    [ReadOnly]
    public NativeArray<float> Heightmap;
    /// <summary>
    /// Heightmap side size
    /// </summary>
    [ReadOnly]
    public int HeightmapSize;
    /// <summary>
    /// Precomputed row vertices row lengths
    /// </summary>
    [ReadOnly]
    public NativeArray<int> VxRowSize;
    /// <summary>
    /// Map's sea level in range 0 to 1
    /// </summary>
    [ReadOnly]
    public float SeaLevel;
    /// <summary>
    /// How much detail to skip in the middle of the mesh, 1 = highest quality, 2 = 50% lower..
    /// </summary>
    [ReadOnly]
    public int DetailIncrement;

    /// <summary>
    /// Mesh vertices, length = mesh edge vertices + main vertices + edge connection vertices
    /// </summary>
    [WriteOnly]
    public NativeArray<float3> Vertices;
    /// <summary>
    /// Mesh normals, same length as Vertices
    /// </summary>
    [WriteOnly]
    public NativeArray<float3> Normals;
    /// <summary>
    /// Mesh quad definition for mesh, same length as Vertices  ////(width-1)*(height-1)*6
    /// </summary>
    [WriteOnly]
    public NativeArray<MeshQuad> Quads;

    public void Execute(int index)
    {
        // index = Vertices index!!!!!!!!!
        // coordinates within vertice array assuming ALL vertices will be used (except out of mesh)
        var inI = CalculateComplexInnerIndex(index);
        var rsize = VxRowSize[0];
        var hmapIndex = (inI.y + 1) * HeightmapSize + inI.x + 1;
        var vertType = CalculateVerticeType(inI);

        /// VERTICE COORDINATES ///
        var vC2 = new float2(inI.x / (float)Constants.PROVINCE_RESOLUTION, inI.y / (float)Constants.PROVINCE_RESOLUTION);
        //var vC2 = inI;
        if ((vertType & VerticeType.EdgeConnection) != 0 && (vertType & VerticeType.Main) == 0)
        {
            // edge connection BUT not a main vertice
            if (inI.y == 1 || inI.y == rsize - 2)
            {
                // horizontal
                var xprog = (inI.x-1) % DetailIncrement / (float)DetailIncrement;
                var x1 = new int2((inI.x - 1) / DetailIncrement * DetailIncrement + 1, inI.y);
                var x2 = x1 + new int2(DetailIncrement, 0);
                var lerped = math.lerp(GetHeightAt(x1), GetHeightAt(x2), xprog);
                Vertices[index] = new(vC2.x, lerped, vC2.y);
            } else
            {
                // vertical
                var yprog = (inI.y-1) % DetailIncrement / (float)DetailIncrement;
                var y1 = new int2(inI.x, (inI.y - 1) / DetailIncrement * DetailIncrement + 1);
                var y2 = y1 + new int2(0, DetailIncrement);
                var lerped = math.lerp(GetHeightAt(y1), GetHeightAt(y2), yprog);
                Vertices[index] = new(vC2.x, lerped, vC2.y);
            }
        } else
        {
            // not edge connection, using normal height
            Vertices[index] = new(vC2.x, Heightmap[hmapIndex], vC2.y);
        }

        /// CALCULATING QUADS ///
        if (math.all(inI < rsize - 1))
        {
            // not on the right side/bottom vertices
            if (math.any(inI == 0) || math.any(inI == rsize - 2))
            {
                // top or second to last row, or 1st column or second to last column
                // these points form highest quality quads
                Quads[index] = new MeshQuad
                {
                    TriOne = new(
                        CalculateVertexIndex(inI + new int2(1, 1)),
                        index,
                        CalculateVertexIndex(inI + new int2(0, 1))
                    ),
                    TriTwo = new(
                        CalculateVertexIndex(inI + new int2(1, 0)),
                        index,
                        CalculateVertexIndex(inI + new int2(1, 1))
                    ),
                    Valid = true
                };
            }
            else if ((vertType & VerticeType.Main) != 0)
            {
                // main vertices (used to lower quality)
                Quads[index] = new MeshQuad
                {
                    TriOne = new(
                        CalculateVertexIndex(inI + new int2(DetailIncrement, DetailIncrement)),
                        index,
                        CalculateVertexIndex(inI + new int2(0, DetailIncrement))
                    ),
                    TriTwo = new(
                        CalculateVertexIndex(inI + new int2(DetailIncrement, 0)),
                        index,
                        CalculateVertexIndex(inI + new int2(DetailIncrement, DetailIncrement))
                    ),
                    Valid = true
                };
            }
            else {
                // Invalid triangle positions,
                Quads[index] = new MeshQuad
                {
                    TriOne = new(),
                    TriTwo = new(),
                    Valid = false
                };
            }
        }

        /// CALCULATING NORMALS (fml) ///
        // hmap indices with movement of 1
        var x0y0 = HmapCoord(hmapIndex); // no movement
        var xm1ym1 = HmapCoord(hmapIndex - HeightmapSize - 1); // x-1;y-1
        var ym1 = HmapCoord(hmapIndex - HeightmapSize); // y-1
        var xm1 = HmapCoord(hmapIndex - 1); // x-1
        var xp1 = HmapCoord(hmapIndex + 1); // x+1
        var yp1 = HmapCoord(hmapIndex + HeightmapSize); // y+1
        var xp1yp1 = HmapCoord(hmapIndex + HeightmapSize + 1); // x+1;y+1
        // normals that often repeat for use cases
        var Anorm = BurstUtilities.CalculateNormal(xm1ym1, x0y0, xm1);
        var Bnorm = BurstUtilities.CalculateNormal(xm1ym1, ym1, x0y0);
        var Cnorm = BurstUtilities.CalculateNormal(xm1, x0y0, yp1);
        var Dnorm = BurstUtilities.CalculateNormal(x0y0, xp1yp1, yp1);
        var Enorm = BurstUtilities.CalculateNormal(x0y0, xp1, xp1yp1);
        var Fnorm = BurstUtilities.CalculateNormal(ym1, xp1, x0y0);
        float3 normSum = new();
        if ((vertType & VerticeType.Edge) != 0)
        {
            // vertices on the edge have max quality to ensure there are no gaps
            // this means that difference between indexes is 1 on each axis
            normSum = Anorm + Bnorm + Cnorm + Dnorm + Enorm + Fnorm;
        }
        else if ((vertType & VerticeType.Main) != 0)
        {
            if (math.all(inI > 1) && math.all(inI < rsize - 2))
            {
                // middle points (same as edge, except with detail increment)
                var xmDymD = HmapCoord(hmapIndex - DetailIncrement * (HeightmapSize + 1)); // x-Detail; y-Detail
                var xmD = HmapCoord(hmapIndex - DetailIncrement); // x-Detail
                var ymD = HmapCoord(hmapIndex - DetailIncrement * HeightmapSize); // y-Detail
                var xpD = HmapCoord(hmapIndex + DetailIncrement); // x+Detail
                var ypD = HmapCoord(hmapIndex + DetailIncrement * HeightmapSize); // y+Detail
                var xpDypD = HmapCoord(hmapIndex + DetailIncrement * (HeightmapSize + 1)); // x+Detail; y+Detail

                Anorm = BurstUtilities.CalculateNormal(xmDymD, x0y0, xmD);
                Bnorm = BurstUtilities.CalculateNormal(xmDymD, ymD, x0y0);
                Cnorm = BurstUtilities.CalculateNormal(xmD, x0y0, ypD);
                Dnorm = BurstUtilities.CalculateNormal(x0y0, xpDypD, ypD);
                Enorm = BurstUtilities.CalculateNormal(x0y0, xpD, xpDypD);
                Fnorm = BurstUtilities.CalculateNormal(ymD, xpD, x0y0);
                normSum = Anorm + Bnorm + Cnorm + Dnorm + Enorm + Fnorm;
            }
            else if (math.all(inI == new int2(1, 1)))
            {
                // top left corner
                var xpD = HmapCoord(hmapIndex + DetailIncrement); // x+Detail
                var ypD = HmapCoord(hmapIndex + DetailIncrement * HeightmapSize); // y+Detail
                var xpDypD = HmapCoord(hmapIndex + DetailIncrement * (HeightmapSize + 1)); // x+Detail; y+Detail
                var Dbot = BurstUtilities.CalculateNormal(x0y0, xpDypD, ypD);
                var Dtop = BurstUtilities.CalculateNormal(x0y0, xpD, xpDypD);
                normSum = Anorm + Bnorm + Cnorm + Fnorm + Dbot + Dtop;
            }
            else if (math.all(inI == new int2(rsize - 2, 1)))
            {
                // top right corner
                var xmD = HmapCoord(hmapIndex - DetailIncrement); // x-Detail
                var ypD = HmapCoord(hmapIndex + DetailIncrement * HeightmapSize); // y + Detail
                var big = BurstUtilities.CalculateNormal(xmD, x0y0, ypD);
                normSum = big + Anorm + Bnorm + Fnorm + Dnorm + Enorm;
            }
            else if (math.all(inI == new int2(1, rsize - 2)))
            {
                // bottom left corner
                var ymD = HmapCoord(hmapIndex - DetailIncrement * HeightmapSize); // y - Detail
                var xpD = HmapCoord(hmapIndex + DetailIncrement); // x + Detail
                var big = BurstUtilities.CalculateNormal(ymD, xpD, x0y0);
                normSum = big + Anorm + Bnorm + Cnorm + Dnorm + Enorm;
            }
            else if (math.all(inI == new int2(rsize - 2, rsize - 2)))
            {
                // bottom right corner
                var xmD = HmapCoord(hmapIndex - DetailIncrement); // x-Detail
                var ymD = HmapCoord(hmapIndex - DetailIncrement * HeightmapSize); // y-Detail
                var xmDymD = HmapCoord(hmapIndex - DetailIncrement * (HeightmapSize + 1)); // x-Detail; y-Detail
                var Dbot = BurstUtilities.CalculateNormal(xmDymD, x0y0, xmD);
                var Dtop = BurstUtilities.CalculateNormal(xmDymD, ymD, x0y0);
                normSum = Dbot + Dtop + Fnorm + Cnorm + Dnorm + Enorm;
            }
            else if (inI.x == 1)
            {
                // left side, corners already covered so not checking for them in if
                var ymD = HmapCoord(hmapIndex - DetailIncrement * HeightmapSize); // y-Detail
                var xpD = HmapCoord(hmapIndex + DetailIncrement); // x+Detail
                var ypD = HmapCoord(hmapIndex + DetailIncrement * HeightmapSize); // y+Detail
                var xpDypD = HmapCoord(hmapIndex + DetailIncrement * (HeightmapSize + 1)); // x+Detail; y+Detail
                var top = BurstUtilities.CalculateNormal(ymD, xpD, x0y0);
                var mid = BurstUtilities.CalculateNormal(x0y0, xpD, xpDypD);
                var bot = BurstUtilities.CalculateNormal(x0y0, xpDypD, ypD);
                normSum = top + mid + bot + Anorm + Bnorm + Cnorm;
            }
            else if (inI.x == rsize - 2) {
                // right side
                var xmDymD = HmapCoord(hmapIndex - DetailIncrement * (HeightmapSize + 1)); // x-Detail; y-Detail
                var xmD = HmapCoord(hmapIndex - DetailIncrement); // x-Detail
                var ymD = HmapCoord(hmapIndex - DetailIncrement * HeightmapSize); // y-Detail
                var ypD = HmapCoord(hmapIndex + DetailIncrement * HeightmapSize); // y+Detail
                var top = BurstUtilities.CalculateNormal(xmDymD, ymD, x0y0);
                var mid = BurstUtilities.CalculateNormal(xmDymD, x0y0, xmD);
                var bot = BurstUtilities.CalculateNormal(xmD, x0y0, ymD);
                normSum = top + mid + bot + Fnorm + Dnorm + Enorm;
            }
            else if (inI.y == 1)
            {
                // top row
                var xmD = HmapCoord(hmapIndex - DetailIncrement); // x-Detail
                var xpD = HmapCoord(hmapIndex + DetailIncrement); // x+Detail
                var ypD = HmapCoord(hmapIndex + DetailIncrement * HeightmapSize); // y+Detail
                var xpDypD = HmapCoord(hmapIndex + DetailIncrement * (HeightmapSize + 1)); // x+Detail; y+Detail
                var left = BurstUtilities.CalculateNormal(xmD, x0y0, ypD);
                var mid = BurstUtilities.CalculateNormal(x0y0, xpDypD, ypD);
                var right = BurstUtilities.CalculateNormal(x0y0, xpD, xpDypD);
                normSum = left + mid + right + Anorm + Bnorm + Fnorm;
            }
            else if (inI.y == rsize - 2)
            {
                // bottom row
                var xmDymD = HmapCoord(hmapIndex - DetailIncrement * (HeightmapSize + 1)); // x-Detail; y-Detail
                var xmD = HmapCoord(hmapIndex - DetailIncrement); // x-Detail
                var ymD = HmapCoord(hmapIndex - DetailIncrement * HeightmapSize); // y-Detail
                var xpD = HmapCoord(hmapIndex + DetailIncrement); // x+Detail
                var left = BurstUtilities.CalculateNormal(xmDymD, x0y0, xmD);
                var mid = BurstUtilities.CalculateNormal(xmDymD, ymD, x0y0);
                var right = BurstUtilities.CalculateNormal(ymD, xpD, x0y0);
                normSum = left + mid + right + Cnorm + Dnorm + Enorm;
            }
        } else if ((vertType & VerticeType.EdgeConnection) != 0) {
            // left side
            normSum = math.select(normSum, Anorm + Bnorm + Cnorm, inI.x == 1);
            // right side
            normSum = math.select(normSum, Fnorm + Dnorm + Enorm, inI.x == rsize - 2);
            // top row
            normSum = math.select(normSum, Anorm + Bnorm + Fnorm, inI.y == 1);
            // bottom row
            normSum = math.select(normSum, Cnorm + Dnorm + Enorm, inI.y == rsize - 2);
        }
        Normals[index] = math.normalize(normSum);
    }

    [Flags]
    private enum VerticeType
    {
        Unset = 0,
        Skipped = 1,
        Edge = 2,
        EdgeConnection = 4,
        Main = 8
    }

    private readonly float3 HmapCoord(int index)
    {
        return new(index % HeightmapSize, GetHeightAt(index), index / HeightmapSize);
    }

    private readonly float GetHeightAt(int2 coord)
    {
        var hmapIndex = (coord.y + 1) * HeightmapSize + coord.x + 1;
        return Heightmap[hmapIndex];
    }
    private readonly float GetHeightAt(int hmapIndex)
    {
        return Heightmap[hmapIndex]; //math.max(SeaLevel, Heightmap[hmapIndex]);
    }

    private readonly VerticeType CalculateVerticeType(int2 innerI) {
        var rsize = VxRowSize[0];
        var vertType = VerticeType.Unset;
        vertType |= (VerticeType)math.select(
            // checking if vertice is edge
            (int)vertType,
            (int)VerticeType.Edge,
            math.any(innerI == 0) || math.any(innerI == rsize-1)
        );
        vertType |= (VerticeType)math.select(
            // checking if vertice is edge connection
            (int)vertType,
            (int)VerticeType.EdgeConnection,
            (vertType & VerticeType.Edge) == 0 &&
            (innerI.x == 1 || innerI.x == rsize - 2 || innerI.y == 1 || innerI.y == rsize - 2)
        );
        vertType |= (VerticeType)math.select(
            (int)vertType,
            (int)VerticeType.Main,
            (vertType & VerticeType.Edge) == 0 &&
            ((innerI.x - 1) % DetailIncrement == 0 && (innerI.y - 1) % DetailIncrement == 0)
        );
        vertType |= (VerticeType)math.select(
            (int)vertType,
            (int)VerticeType.Skipped,
            (vertType & VerticeType.Main) == 0 &&
            (innerI.x > 1 && innerI.x < rsize - 2) &&
            (innerI.y > 1 && innerI.y < rsize - 2)
        );

        return vertType;
    }

    private readonly int CalculateVertexIndex(int2 coordinate) {
        var rsize = VxRowSize[0];
        var rowSum = 0;
        // counting up all the rows leading up to current one
        for (int row = 0; row < coordinate.y; row++)
        {
            rowSum += VxRowSize[row];
        }
        // iterating by 1 till finding matching x
        var vxI = rowSum;
        for (int gridX = 0; gridX < rsize; gridX++)
        {
            if (gridX == coordinate.x)
                return vxI;

            var vT = CalculateVerticeType(new(gridX, coordinate.y));
            if ((vT & VerticeType.Skipped) == 0)
            {
                vxI++;
            }
        }
        return vxI;
    }

    private readonly int2 CalculateComplexInnerIndex(int index) {
        int2 innerI = new(-1, -1);
        var rsize = VxRowSize[0];
        // best quality wasnt selected, harder calculation :/
        innerI = math.select(
            math.select(
                // calculate innerI if bottom 2 rows, otherwise leave unchanged
                innerI,
                new(
                    (index - (Vertices.Length - rsize * 2)) % rsize,
                    (index - (Vertices.Length - rsize * 2)) / rsize + rsize-2
                ),
                index >= Vertices.Length - rsize * 2
            ),
            // top 2 rows
            new(index % rsize, index / rsize),
            index < rsize * 2
        );

        var sum = rsize * 2;
        for (int row = 2; row < VxRowSize.Length - 2; row++)
        {
            // whether it's this row
            var rowVx = VxRowSize[row]; // vertices for this row
            var thisRow = index < sum + rowVx && index >= sum;
            innerI.y = math.select(innerI.y, row, thisRow);
            // checking whether its main vertice row, if yes doing extra calculation for X
            var mainVertRow = thisRow && rowVx != 4;

            var curRowI = index - sum; // index on current row
            innerI.x = math.select(
                // setting X if row has 4 vertices (only sides)
                innerI.x,
                math.select(
                    // not main vertices row, aka 4 vertices (2 on each side)
                    rsize + curRowI - 4,
                    curRowI,
                    curRowI < 2
                ),
                thisRow && !mainVertRow
            );

            innerI.x = math.select(
                // setting X if row has more than 4 vertices (main vertices + edges)
                innerI.x,
                math.select(
                    math.select(
                        (curRowI - 1) * DetailIncrement + 1, // middle
                        // right side
                        rsize + curRowI - rowVx,
                        curRowI >= rowVx - 2
                    ),
                    // left side
                    curRowI,
                    curRowI < 2
                ),
                thisRow && mainVertRow
            );

            sum += VxRowSize[row];
            // if Y condition is true, this acts as a break
            row = math.select(row, VxRowSize.Length, thisRow);
        }

        return innerI;
    }
}
// at which point i shouldve realised that
// i shouldve just written a few short single threaded loops
// instead of this, just like in the video?