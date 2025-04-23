/// <summary>
/// Small class to act as user input action singleton
/// </summary>
public static class UserInputController
{
    private static UserInput userInputActions;

    public static UserInput GetUserInputActions()
    {
        if (userInputActions != null)
            return userInputActions;

        userInputActions = new UserInput();
        userInputActions.Enable();
        return userInputActions;
    }
}
