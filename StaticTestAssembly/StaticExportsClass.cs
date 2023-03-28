namespace StaticTestAssembly;

public class StaticExportsClass
{
    public static int TestFunction(string value)
    {
        return value.Length;
    }
}