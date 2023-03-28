namespace DynamicTestAssembly;

public class DynamicExportsClass
{
    public static int TestFunction(string value)
    {
        return value.Length;
    }
}