
namespace Janus;

public partial class Shared
{
    public static void log(object obj)
    {
        Console.WriteLine("{0}", obj.JsonSerialize());
    }
}
