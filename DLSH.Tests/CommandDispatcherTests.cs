namespace DLSH.Tests;
public class CommandDispatcherTests
{
    [Fact]
    public void Dispatch_AliasExpansion_Works()
    {
        var aliasService = new AliasService();
        aliasService.Add("g", "git");
        var dispatcher = new CommandDispatcher(aliasService);

        dispatcher.Dispatch("g status");
    }

    [Fact]
    public void Dispatch_RecursiveAlias_ThrowsLimitError()
    {
        var aliasService = new AliasService();
        aliasService.Add("a", "b");
        aliasService.Add("b", "a");
        var dispatcher = new CommandDispatcher(aliasService);

        var exception = Record.Exception(() => dispatcher.Dispatch("a"));
        Assert.Null(exception);
    }
}