﻿using System;

namespace Penguin_Pairs
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new PenguinPairs())
                game.Run();
        }
    }
}
