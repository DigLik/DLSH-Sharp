namespace DLSH.Tests;
public class AliasServiceTests
{
    [Fact]
    public void Add_And_Get_Alias_Success()
    {
        var service = new AliasService();
        service.Add("ll", "ls -la");

        var result = service.Get("ll");

        Assert.Equal("ls -la", result);
    }

    [Fact]
    public void Remove_Alias_Success()
    {
        var service = new AliasService();
        service.Add("temp", "echo 123");

        service.Remove("temp");
        var result = service.Get("temp");

        Assert.Null(result);
    }
}