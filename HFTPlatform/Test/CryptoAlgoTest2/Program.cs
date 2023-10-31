
internal class Program
{
    private static void DoTestConn()
    {
        OrderRouter router = new OrderRouter();
            
        router.TestRouting();
//            
    }

        
    public static void Main(string[] args)
    {
        DoTestConn();
    }
}