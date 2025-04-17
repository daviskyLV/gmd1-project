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
    /// UV map for mesh, same length as Vertices
    /// </summary>
    [WriteOnly]
    public NativeArray<float2> UVs;
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
        int2 innerI = CalculateComplexInnerIndex(index);
        var rsize = VxRowSize[0];
        var hmapIndex = (innerI.y + 1) * HeightmapSize + innerI.x + 1;
        var vertType = CalculateVerticeType(innerI);

        /// VERTICE COORDINATES ///
        Vertices[index] = math.select(
            math.select(
                // edge connection, interpolating height
                // vertical
                new float3(innerI.x,
                    math.max(SeaLevel, (innerI.y % DetailIncrement) / (float)DetailIncrement),
                    innerI.y),
                // horizontal
                new float3(innerI.x,
                    math.max(SeaLevel, (innerI.x % DetailIncrement) / (float)DetailIncrement),
                    innerI.y),
                innerI.y == 1 || innerI.y == rsize-2
            ),
            // not edge connection, using normal height
            new float3(innerI.x, math.max(SeaLevel, Heightmap[hmapIndex]), innerI.y),
            // edge connection BUT not a main vertice (inverted)
            !((vertType & VerticeType.EdgeConnection) != 0 && (vertType & VerticeType.Main) == 0)
        );
        /// UV MAP ///
        UVs[index] = new(innerI.x / (float)rsize, innerI.y / (float)rsize);

        /// CALCULATING QUADS ///
        if (math.all(innerI < rsize - 1))
        {
            // not on the right side/bottom vertices
            if (math.any(innerI == 0) || math.any(innerI == rsize - 2))
            {
                // top or second to last row, or 1st column or second to last column
                // these points form highest quality quads
                Quads[index] = new MeshQuad
                {
                    TriOne = new(
                        index,
                        CalculateVertexIndex(innerI + new int2(1, 1)),
                        CalculateVertexIndex(innerI + new int2(0, 1))
                    ),
                    TriTwo = new(
                        index,
                        CalculateVertexIndex(innerI + new int2(1, 0)),
                        CalculateVertexIndex(innerI + new int2(1, 1))
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
                        index,
                        CalculateVertexIndex(innerI + new int2(DetailIncrement, DetailIncrement)),
                        CalculateVertexIndex(innerI + new int2(0, DetailIncrement))
                    ),
                    TriTwo = new(
                        index,
                        CalculateVertexIndex(innerI + new int2(DetailIncrement, 0)),
                        CalculateVertexIndex(innerI + new int2(DetailIncrement, DetailIncrement))
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
        var x0y0 = Heightmap[hmapIndex]; // no movement
        var xm1ym1 = Heightmap[hmapIndex - HeightmapSize - 1]; // x-1;y-1
        var ym1 = Heightmap[hmapIndex - HeightmapSize]; // y-1
        var xm1 = Heightmap[hmapIndex - 1]; // x-1
        var xp1 = Heightmap[hmapIndex + 1]; // x+1
        var yp1 = Heightmap[hmapIndex + HeightmapSize]; // y+1
        var xp1yp1 = Heightmap[hmapIndex + HeightmapSize + 1]; // x+1;y+1
        // normals that often repeat for use cases
        var Anorm = Utilities.CalculateNormal(xm1ym1, x0y0, xm1);
        var Bnorm = Utilities.CalculateNormal(xm1ym1, ym1, x0y0);
        var Cnorm = Utilities.CalculateNormal(xm1, x0y0, yp1);
        var Dnorm = Utilities.CalculateNormal(x0y0, xp1yp1, yp1);
        var Enorm = Utilities.CalculateNormal(x0y0, xp1, xp1yp1);
        var Fnorm = Utilities.CalculateNormal(ym1, xp1, x0y0);
        float3 normSum = new();
        if ((vertType & VerticeType.Edge) != 0)
        {
            // vertices on the edge have max quality to ensure there are no gaps
            // this means that difference between indexes is 1 on each axis
            normSum = Anorm + Bnorm + Cnorm + Dnorm + Enorm + Fnorm;
        }
        else if ((vertType & VerticeType.Main) != 0)
        {
            if (math.all(innerI > 1) && math.all(innerI < rsize - 2))
            {
                // middle points (same as edge, except with detail increment)
                var xmDymD = Heightmap[hmapIndex - DetailIncrement * (HeightmapSize + 1)]; // x-Detail; y-Detail
                var xmD = Heightmap[hmapIndex - DetailIncrement]; // x-Detail
                var ymD = Heightmap[hmapIndex - DetailIncrement * HeightmapSize]; // y-Detail
                var xpD = Heightmap[hmapIndex + DetailIncrement]; // x+Detail
                var ypD = Heightmap[hmapIndex + DetailIncrement * HeightmapSize]; // y+Detail
                var xpDypD = Heightmap[hmapIndex + DetailIncrement * (HeightmapSize + 1)]; // x+Detail; y+Detail

                Anorm = Utilities.CalculateNormal(xmDymD, x0y0, xmD);
                Bnorm = Utilities.CalculateNormal(xmDymD, ymD, x0y0);
                Cnorm = Utilities.CalculateNormal(xmD, x0y0, ypD);
                Dnorm = Utilities.CalculateNormal(x0y0, xpDypD, ypD);
                Enorm = Utilities.CalculateNormal(x0y0, xpD, xpDypD);
                Fnorm = Utilities.CalculateNormal(ymD, xpD, x0y0);
                normSum = Anorm + Bnorm + Cnorm + Dnorm + Enorm + Fnorm;
            }
            else if (math.all(innerI == new int2(1, 1)))
            {
                // top left corner
                var xpD = Heightmap[hmapIndex + DetailIncrement]; // x+Detail
                var ypD = Heightmap[hmapIndex + DetailIncrement * HeightmapSize]; // y+Detail
                var xpDypD = Heightmap[hmapIndex + DetailIncrement * (HeightmapSize + 1)]; // x+Detail; y+Detail
                var Dbot = Utilities.CalculateNormal(x0y0, xpDypD, ypD);
                var Dtop = Utilities.CalculateNormal(x0y0, xpD, xpDypD);
                normSum = Anorm + Bnorm + Cnorm + Fnorm + Dbot + Dtop;
            }
            else if (math.all(innerI == new int2(rsize - 2, 1)))
            {
                // top right corner
                var xmD = Heightmap[hmapIndex - DetailIncrement]; // x-Detail
                var ypD = Heightmap[hmapIndex + DetailIncrement * HeightmapSize]; // y + Detail
                var big = Utilities.CalculateNormal(xmD, x0y0, ypD);
                normSum = big + Anorm + Bnorm + Fnorm + Dnorm + Enorm;
            }
            else if (math.all(innerI == new int2(1, rsize - 2)))
            {
                // bottom left corner
                var ymD = Heightmap[hmapIndex - DetailIncrement * HeightmapSize]; // y - Detail
                var xpD = Heightmap[hmapIndex + DetailIncrement]; // x + Detail
                var big = Utilities.CalculateNormal(ymD, xpD, x0y0);
                normSum = big + Anorm + Bnorm + Cnorm + Dnorm + Enorm;
            }
            else if (math.all(innerI == new int2(rsize - 2, rsize - 2)))
            {
                // bottom right corner
                var xmD = Heightmap[hmapIndex - DetailIncrement]; // x-Detail
                var ymD = Heightmap[hmapIndex - DetailIncrement * HeightmapSize]; // y-Detail
                var xmDymD = Heightmap[hmapIndex - DetailIncrement * (HeightmapSize + 1)]; // x-Detail; y-Detail
                var Dbot = Utilities.CalculateNormal(xmDymD, x0y0, xmD);
                var Dtop = Utilities.CalculateNormal(xmDymD, ymD, x0y0);
                normSum = Dbot + Dtop + Fnorm + Cnorm + Dnorm + Enorm;
            }
            else if (innerI.x == 1)
            {
                // left side, corners already covered so not checking for them in if
                var ymD = Heightmap[hmapIndex - DetailIncrement * HeightmapSize]; // y-Detail
                var xpD = Heightmap[hmapIndex + DetailIncrement]; // x+Detail
                var ypD = Heightmap[hmapIndex + DetailIncrement * HeightmapSize]; // y+Detail
                var xpDypD = Heightmap[hmapIndex + DetailIncrement * (HeightmapSize + 1)]; // x+Detail; y+Detail
                var top = Utilities.CalculateNormal(ymD, xpD, x0y0);
                var mid = Utilities.CalculateNormal(x0y0, xpD, xpDypD);
                var bot = Utilities.CalculateNormal(x0y0, xpDypD, ypD);
                normSum = top + mid + bot + Anorm + Bnorm + Cnorm;
            }
            else if (innerI.x == rsize - 2) {
                // right side
                var xmDymD = Heightmap[hmapIndex - DetailIncrement * (HeightmapSize + 1)]; // x-Detail; y-Detail
                var xmD = Heightmap[hmapIndex - DetailIncrement]; // x-Detail
                var ymD = Heightmap[hmapIndex - DetailIncrement * HeightmapSize]; // y-Detail
                var ypD = Heightmap[hmapIndex + DetailIncrement * HeightmapSize]; // y+Detail
                var top = Utilities.CalculateNormal(xmDymD, ymD, x0y0);
                var mid = Utilities.CalculateNormal(xmDymD, x0y0, xmD);
                var bot = Utilities.CalculateNormal(xmD, x0y0, ymD);
                normSum = top + mid + bot + Fnorm + Dnorm + Enorm;
            }
            else if (innerI.y == 1)
            {
                // top row
                var xmD = Heightmap[hmapIndex - DetailIncrement]; // x-Detail
                var xpD = Heightmap[hmapIndex + DetailIncrement]; // x+Detail
                var ypD = Heightmap[hmapIndex + DetailIncrement * HeightmapSize]; // y+Detail
                var xpDypD = Heightmap[hmapIndex + DetailIncrement * (HeightmapSize + 1)]; // x+Detail; y+Detail
                var left = Utilities.CalculateNormal(xmD, x0y0, ypD);
                var mid = Utilities.CalculateNormal(x0y0, xpDypD, ypD);
                var right = Utilities.CalculateNormal(x0y0, xpD, xpDypD);
                normSum = left + mid + right + Anorm + Bnorm + Fnorm;
            }
            else if (innerI.y == rsize - 2)
            {
                // bottom row
                var xmDymD = Heightmap[hmapIndex - DetailIncrement * (HeightmapSize + 1)]; // x-Detail; y-Detail
                var xmD = Heightmap[hmapIndex - DetailIncrement]; // x-Detail
                var ymD = Heightmap[hmapIndex - DetailIncrement * HeightmapSize]; // y-Detail
                var xpD = Heightmap[hmapIndex + DetailIncrement]; // x+Detail
                var left = Utilities.CalculateNormal(xmDymD, x0y0, xmD);
                var mid = Utilities.CalculateNormal(xmDymD, ymD, x0y0);
                var right = Utilities.CalculateNormal(ymD, xpD, x0y0);
                normSum = left + mid + right + Cnorm + Dnorm + Enorm;
            }
        } else if ((vertType & VerticeType.EdgeConnection) != 0) {
            // left side
            normSum = math.select(normSum, Anorm + Bnorm + Cnorm, innerI.x == 1);
            // right side
            normSum = math.select(normSum, Fnorm + Dnorm + Enorm, innerI.x == rsize - 2);
            // top row
            normSum = math.select(normSum, Anorm + Bnorm + Fnorm, innerI.y == 1);
            // bottom row
            normSum = math.select(normSum, Cnorm + Dnorm + Enorm, innerI.y == rsize - 2);
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

    private readonly VerticeType CalculateVerticeType(int2 innerI) {
        var rsize = VxRowSize[0];
        var vertType = VerticeType.Unset;
        vertType |= (VerticeType)math.select(
            // checking if vertice is edge
            (int)vertType,
            (int)VerticeType.Edge,
            innerI.x == 0 || innerI.x == rsize - 1 || innerI.y == 0 || innerI.y == rsize - 1
        );
        vertType |= (VerticeType)math.select(
            // checking if vertice is edge connection
            (int)vertType,
            (int)VerticeType.EdgeConnection,
            (innerI.x == 1 || innerI.x == rsize - 2) && (innerI.y == 1 || innerI.y == rsize - 2)
        );
        vertType |= (VerticeType)math.select(
            (int)vertType,
            (int)VerticeType.Main,
            (vertType & VerticeType.Edge) == 0 && ((innerI.x - 1) % DetailIncrement == 0 || (innerI.y - 1) % DetailIncrement == 0)
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
                    (index - rsize * 2) % rsize,
                    (index - rsize * 2) / rsize + rsize - 2
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
            var thisRow = index < sum + VxRowSize[row];
            innerI.y = math.select(innerI.y, row, thisRow);
            // checking whether its main vertice row, if yes doing extra calculation for X
            var mainVertRow = thisRow && VxRowSize[row] != 4;

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
                        rsize + curRowI - VxRowSize[row],
                        curRowI >= VxRowSize[row] - 2
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