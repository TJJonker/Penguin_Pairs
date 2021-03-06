﻿using Engine;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Penguin_Pairs
{
    internal class PenguinPairs : ExtendedGame
    {
        public const string StateName_Title = "TitleScreen";
        public const string StateName_Help = "HelpScreen";
        public const string StateName_LevelMenu = "LevelMenuScreen";
        public const string StateName_Options = "OptionsScreen";
        public const string StateName_Playing = "PlayingScreen";
        public static bool HintsEnabled { get; set; }

        private static List<LevelStatus> progress;

        public static int NumberOfLevels { get { return progress.Count; } }

        public static LevelStatus GetLevelStatus(int levelIndex)
        {
            return progress[levelIndex - 1];
        }

        private static void SetLevelStatus(int levelIndex, LevelStatus status)
        {
            progress[levelIndex - 1] = status;
        }

        [STAThread]
        private static void Main()
        {
            PenguinPairs game = new PenguinPairs();
            game.Run();
        }

        public PenguinPairs()
        {
            IsMouseVisible = true;
            HintsEnabled = true;
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            worldSize = new Point(1200, 900);
            windowSize = new Point(1024, 768);

            FullScreen = false;

            LoadProgress();

            GameStateManager.AddGameState(StateName_Title, new TitleMenuState());
            GameStateManager.AddGameState(StateName_Help, new HelpState());
            GameStateManager.AddGameState(StateName_LevelMenu, new LevelMenuState());
            GameStateManager.AddGameState(StateName_Options, new OptionsMenuState());
            GameStateManager.AddGameState(StateName_Playing, new PlayingState());

            GameStateManager.SwitchTo(StateName_Title);

            AssetManager.PlaySong("Sounds/snd_music", true);
        }

        private void LoadProgress()
        {
            progress = new List<LevelStatus>();
            StreamReader reader = new StreamReader("Content/Levels/levels_status.txt");
            string line = reader.ReadLine();
            while(line != null)
            {
                progress.Add(TextToLevelStatus(line));
                line = reader.ReadLine();
            }
            reader.Close();
        }

        private static void SaveProgress()
        {
            StreamWriter w = new StreamWriter("Content/Levels/level_status.txt");
            foreach(LevelStatus status in progress)
            {
                if (status == LevelStatus.Locked)
                    w.WriteLine("locked");
                else if (status == LevelStatus.Unlocked)
                    w.WriteLine("unlocked");
                else w.WriteLine("solved");
            }
            w.Close();
        }

        public static void MarkLevelAsSolved(int levelIndex)
        {
            SetLevelStatus(levelIndex, LevelStatus.Solved);
            if (levelIndex < NumberOfLevels && GetLevelStatus(levelIndex + 1) == LevelStatus.Locked)
                SetLevelStatus(levelIndex + 1, LevelStatus.Unlocked);

            SaveProgress();
        }

        private LevelStatus TextToLevelStatus(string text)
        {
            if (text == "locked") return LevelStatus.Locked;
            if (text == "unlocked") return LevelStatus.Unlocked;
            return LevelStatus.Solved;
        }

        public static void GoToNextLevel(int levelIndex)
        {
            if (levelIndex == NumberOfLevels)
                GameStateManager.SwitchTo(StateName_LevelMenu);

            else
            {
                PlayingState playingState = (PlayingState)GameStateManager.GetGameState(StateName_Playing);
                playingState.LoadLevel(levelIndex + 1);
            }
        }
    }
}