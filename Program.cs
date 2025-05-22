using OpenTK.Windowing.Desktop;
using GolfGame;

class Program
{
    static void Main(string[] args)
    {
        using (gGame game = new gGame(800, 400, "Golf Game"))
        {
            game.Run();
        }

    }

}