
using System;

public class MyQueue
{

    public MyQueue(int size)
    {
        Input = new Stack<long>(size);
        Output = new Stack<long>(size);


    }

    protected Stack<long> Input { get; set; }
    protected Stack<long> Output { get; set; }


    public void Enqueue(long data)
    {
        Input.Push(data);

    }

    public long Peek() {
        //Output.Clear();

        long data = -1;
        long toReturn=-1;
        while (Input.Count != 0)
        {

            data = Input.Pop();
            if(Input.Count == 0)
            {
                toReturn = data;


            }
            Output.Push(data);//we skip the last element
        }

        while (Output.Count != 0)
        {
            Input.Push(Output.Pop());
        }

        return toReturn;

    }

    public long Dequeue()
    {
        //Output.Clear();

        long data = -1;
        while(Input.Count!=0)
        {

             data=Input.Pop();
            if(Input.Count>0)
                Output.Push(data);//we skip the last element
        }

        while(Output.Count!=0)
        {
            Input.Push(Output.Pop());
        }

        return data;
    }


}

internal class Excercise
{
    protected static MyQueue MyQueue { get; set; }

    protected static bool FirstLine = true;

    protected static void DoProcess(string[] args)
    {
        try
        {
            if (FirstLine && args.Length == 1)
            {//Input queue size

                FirstLine = false;
                MyQueue = new MyQueue(Convert.ToInt32(args[0]));

            }
            else if (args.Length == 1 || args.Length == 2)
            {
                if (args[0] == "1")//enqueue
                {
                    MyQueue.Enqueue(Convert.ToInt64(args[1]));
                }
                else if (args[0] == "2")//dequeue
                {
                    MyQueue.Dequeue();
                }
                else if (args[0] == "3" && args.Length == 1)//print
                {
                    Console.WriteLine(MyQueue.Peek());
                }

            }
            else
            {
                Console.WriteLine("Invalid args");

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on input length {args.Length}:{ex.Message}");
        
        }


    }

    private static void RunExcercise(string[] args)

    {

        var lines = File.ReadAllLines("input.txt");

        foreach(string line in lines)
        {

            DoProcess(line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries));

        }


        //string cmd = "";
        //while (cmd != "exit")
        //{
        //    cmd=Console.ReadLine();
        //    DoProcess(cmd.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries));

        //}

        //bool firstLine = true;
        //foreach (string[] arg in paramets)
        //{
        //    DoProcess(arg);
        //    firstLine = false;
        //}


        Console.ReadKey();
    }
}