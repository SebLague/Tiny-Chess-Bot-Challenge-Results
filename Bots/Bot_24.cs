namespace auto_Bot_24;
using ChessChallenge.API;
using System;

public class Bot_24 : IChessBot
{
    /*
 
    Let me tell something about this.
 
    firstly: token system sucks with strings. Idk why this happening, but it's
    secondly: It's not a serious bot (Unexpectedly, isn't it?) I'm going to create new sumbit/update this one later (or am I? Idk yet)
    
    Btw, here's python code to generate this whole circus:
    {
    tmplate = """void WaitFor{sec}Seconds()
    {left}
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds({waitFor}))
        {left}
            // Hi from Belarus :3
        {right}
        WaitFor{secnext}Seconds();
    {right}\n\n"""
 
 
    with open("someFile.txt", "w") as f:
        for i in range(59, 12, -2):
            f.write(tmplate.format(
                sec = i,
                secnext = i - 2,
                waitFor = 2,
                left = "{",
                right = "}")
            )
        for i in range(12, 0, -1):
            f.write(tmplate.format(
                sec = i,
                secnext = i - 1,
                waitFor = 1,
                left = "{",
                right = "}")
            )
    }
 
    ...Not my best code, but it doing it's job.
    So, hope you enjoy :3
    */
    public Move Think(Board board, Timer timer)
    {


        WaitFor59Seconds();

        return Move.NullMove;
    }
    void WaitFor59Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor57Seconds();
    }

    void WaitFor57Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor55Seconds();
    }

    void WaitFor55Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor53Seconds();
    }

    void WaitFor53Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor51Seconds();
    }

    void WaitFor51Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor49Seconds();
    }

    void WaitFor49Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor47Seconds();
    }

    void WaitFor47Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor45Seconds();
    }

    void WaitFor45Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor43Seconds();
    }

    void WaitFor43Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor41Seconds();
    }

    void WaitFor41Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor39Seconds();
    }

    void WaitFor39Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor37Seconds();
    }

    void WaitFor37Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor35Seconds();
    }

    void WaitFor35Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor33Seconds();
    }

    void WaitFor33Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor31Seconds();
    }

    void WaitFor31Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor29Seconds();
    }

    void WaitFor29Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor27Seconds();
    }

    void WaitFor27Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor25Seconds();
    }

    void WaitFor25Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor23Seconds();
    }

    void WaitFor23Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor21Seconds();
    }

    void WaitFor21Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor19Seconds();
    }

    void WaitFor19Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor17Seconds();
    }

    void WaitFor17Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor15Seconds();
    }

    void WaitFor15Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor13Seconds();
    }

    void WaitFor13Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            // Hi from Belarus :3
        }
        WaitFor11Seconds();
    }

    void WaitFor12Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(1))
        {
            // Hi from Belarus :3
        }
        WaitFor11Seconds();
    }

    void WaitFor11Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(1))
        {
            // Hi from Belarus :3
        }
        WaitFor10Seconds();
    }

    void WaitFor10Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(1))
        {
            // Hi from Belarus :3
        }
        WaitFor9Seconds();
    }

    void WaitFor9Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(1))
        {
            // Hi from Belarus :3
        }
        WaitFor8Seconds();
    }

    void WaitFor8Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(1))
        {
            // Hi from Belarus :3
        }
        WaitFor7Seconds();
    }

    void WaitFor7Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(1))
        {
            // Hi from Belarus :3
        }
        WaitFor6Seconds();
    }

    void WaitFor6Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(1))
        {
            // Hi from Belarus :3
        }
        WaitFor5Seconds();
    }

    void WaitFor5Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(1))
        {
            // Hi from Belarus :3
        }
        WaitFor4Seconds();
    }

    void WaitFor4Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(1))
        {
            // Hi from Belarus :3
        }
        WaitFor3Seconds();
    }

    void WaitFor3Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(1))
        {
            // Hi from Belarus :3
        }
        WaitFor2Seconds();
    }

    void WaitFor2Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(1))
        {
            // Hi from Belarus :3
        }
        WaitFor1Seconds();
    }

    void WaitFor1Seconds()
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(1))
        {
            // Hi from Belarus :3
        }
    }

    void JustFunctionToBeat64Tokens()
    {
        string phraze = "Just testing counting tokens function";
        string wow = "Bro... j j j";
        return; // The last one <3
    }

}