namespace DLSH.Tests;
public class VariableServiceTests
{
    [Fact]
    public void Set_And_Get_Variable()
    {
        VariableService.Set("TEST_VAR", "Hello"); 
        var value = VariableService.Get("TEST_VAR");

        Assert.Equal("Hello", value);
    }

    [Fact]
    public void HandleVarCommand_Math_Evaluation()
    {
        string[] args = ["match", "RESULT", "=", "2", "+", "2"];

        VariableService.HandleVarCommand(args);
        var result = VariableService.Get("RESULT");

        Assert.Equal("4", result);
    }
}