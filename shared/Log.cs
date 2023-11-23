
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

public partial class Shared
{
    public static void log(Object obj)
    {
        Console.WriteLine("{0}", JsonConvert.SerializeObject(obj));
    }
}
