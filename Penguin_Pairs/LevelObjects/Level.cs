﻿using Engine;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;

namespace Penguin_Pairs
{
    internal class Level : GameObjectList
    {
        private const int TileWidth = 73;
        private const int TileHeight = 72;

        private const string MovableAnimalLetters = "brgycpmx";

        private MovableAnimalSelector selector;

        private PairList pairList;
        public int LevelIndex { get; private set; }
        private int targetNuberOfPairs;

        private Tile[,] tiles;
        private Animal[,] animalsOnTiles;

        private VisibilityTimer hintTimer;
        
        private SpriteGameObject hintArrow;

        private int GridWidth
        { get { return tiles.GetLength(0); } }

        private int GridHeight
        { get { return tiles.GetLength(1); } }

        public bool FirstMoveMade { get; private set; }

        public Level(int levelIndex, string filename)
        {
            LevelIndex = levelIndex;
            LoadLevelFromFile(filename);
            FirstMoveMade = false;
        }

        public override void Reset()
        {
            for (int y = 0; y < GridHeight; y++)
                for (int x = 0; x < GridWidth; x++)
                    animalsOnTiles[x, y] = null;

            FirstMoveMade = false;
            base.Reset();
        }

        public Vector2 GetCellPosition(int x, int y)
        {
            return new Vector2(x * TileWidth, y * TileHeight);
        }

        private void LoadLevelFromFile(string filename)
        {
            StreamReader reader = new StreamReader(filename);

            string title = reader.ReadLine();
            string desciption = reader.ReadLine();

            targetNuberOfPairs = int.Parse(reader.ReadLine());

            AddLevelInfoObjects(title, desciption);

            string[] hint = reader.ReadLine().Split(' ');
            int hintX = int.Parse(hint[0]);
            int hintY = int.Parse(hint[1]);
            int hintDirection = StringToDirection(hint[2]);

            hintArrow = new SpriteGameObject("Sprites/LevelObjects/spr_arrow_hint@4", hintDirection);
            hintArrow.Position = GetCellPosition(hintX, hintY);

            int gridWidth = 0;

            List<string> gridRows = new List<string>();
            string line = reader.ReadLine();
            while (line != null)
            {
                if (line.Length > gridWidth)
                    gridWidth = line.Length;

                gridRows.Add(line);
                line = reader.ReadLine();
            }
            reader.Close();

            AddPlayingField(gridRows, gridWidth, gridRows.Count);
        }

        private void AddLevelInfoObjects(string title, string description)
        {
            // Level info background sprite
            SpriteGameObject levelInfo = new SpriteGameObject("Sprites/spr_level_info");
            levelInfo.SetOriginToCenter();
            levelInfo.Position = new Vector2(600, 820);
            AddChild(levelInfo);

            TextGameObject titleObject = new TextGameObject("Fonts/HelpFont", Color.Blue, TextGameObject.Alignment.Center);
            titleObject.Text = LevelIndex + " - " + title;
            titleObject.Position = new Vector2(600, 786);
            AddChild(titleObject);

            TextGameObject descriptionObject = new TextGameObject("Fonts/HelpFont", Color.Blue, TextGameObject.Alignment.Center);
            descriptionObject.Text = description;
            descriptionObject.Position = new Vector2(600, 820);
            AddChild(descriptionObject);

            pairList = new PairList(targetNuberOfPairs);
            pairList.Position = new Vector2(20, 20);
            AddChild(pairList);
        }

        private void AddPlayingField(List<string> gridRows, int gridWidth, int gridHeight)
        {
            GameObjectList playingField = new GameObjectList();

            Vector2 gridSize = new Vector2(gridWidth * TileWidth, gridHeight * TileHeight);
            playingField.Position = new Vector2(600, 420) - gridSize / 2.0f;

            tiles = new Tile[gridWidth, gridHeight];
            animalsOnTiles = new Animal[gridWidth, gridHeight];

            for (int y = 0; y < gridHeight; y++)
            {
                string row = gridRows[y];
                for (int x = 0; x < gridWidth; x++)
                {
                    char symbol = ' ';
                    if (x < row.Length)
                        symbol = row[x];

                    AddTile(x, y, symbol);
                    AddAnimal(x, y, symbol);
                }
            }

            for (int y = 0; y < gridHeight; y++)
                for (int x = 0; x < gridWidth; x++)
                    playingField.AddChild(tiles[x, y]);

            for (int y = 0; y < gridHeight; y++)
                for (int x = 0; x < gridWidth; x++)
                    if (animalsOnTiles[x, y] != null)
                        playingField.AddChild(animalsOnTiles[x, y]);

            hintArrow.Visible = false;
            playingField.AddChild(hintArrow);
            hintTimer = new VisibilityTimer(hintArrow);
            playingField.AddChild(hintTimer);

            selector = new MovableAnimalSelector();
            playingField.AddChild(selector);

            AddChild(playingField);
        }

        public void ShowHint()
        {
            hintTimer.StartVisible(2);
        }

        public void SelectAnimal(MovableAnimal animal)
        {
            selector.SelectedAnimal = animal;
        }

        public void PairFound(MovableAnimal penguin1, MovableAnimal penguin2)
        {
            int penguinType = MathHelper.Max(penguin1.AnimalIndex, penguin2.AnimalIndex);
            pairList.AddPair(penguinType);

            if (pairList.Completed)
            {
                PlayingState playingState = (PlayingState)ExtendedGame.GameStateManager.GetGameState(PenguinPairs.StateName_Playing);
                playingState.LevelCompleted(LevelIndex);
            }
            else
                ExtendedGame.AssetManager.PlaySoundEffect("Sounds/snd_pair");
        }

        private void AddTile(int x, int y, char symbol)
        {
            Tile tile = new Tile(CharToTileType(symbol), x, y);
            tile.Position = GetCellPosition(x, y);
            tiles[x, y] = tile;
        }

        public Tile.Type GetTileType(Point gridPosition)
        {
            if (!IsPositionInGrid(gridPosition))
                return Tile.Type.Empty;
            return tiles[gridPosition.X, gridPosition.Y].TileType;
        }

        public Animal GetAnimal(Point gridPosition)
        {
            if (!IsPositionInGrid(gridPosition))
                return null;
            return animalsOnTiles[gridPosition.X, gridPosition.Y];
        }

        private void AddAnimal(int x, int y, char symbol)
        {
            Animal result = null;

            // TODO: check if symbol is an animal
            if (symbol == '@') result = new Shark(this, new Point(x, y));

            if (result == null)
            {
                int animalIndex = GetAnimalIndex(symbol);
                if (animalIndex < 0)
                    animalIndex = GetAnimalInHoleIndex(symbol);

                if (animalIndex >= 0)
                    result = new MovableAnimal(animalIndex, this, new Point(x, y));
            }
        }

        public void AddAnimalToGrid(Animal animal, Point gridPosition)
        {
            animalsOnTiles[gridPosition.X, gridPosition.Y] = animal;
        }

        public void RemoveAnimalFromGrid(Point gridPosition)
        {
            animalsOnTiles[gridPosition.X, gridPosition.Y] = null;
            FirstMoveMade = true;
        }

        private int GetAnimalIndex(char symbol)
        {
            return MovableAnimalLetters.IndexOf(symbol);
        }

        private int GetAnimalInHoleIndex(char symbol)
        {
            return MovableAnimalLetters.ToUpper().IndexOf(symbol);
        }

        private int StringToDirection(string direction)
        {
            if (direction == "Right") return 0;
            if (direction == "Up") return 1;
            if (direction == "Left") return 2;
            return 3;
        }

        private Tile.Type CharToTileType(char symbol)
        {
            if (symbol == ' ') return Tile.Type.Empty;
            if (symbol == '.') return Tile.Type.Normal;
            if (symbol == '#') return Tile.Type.Wall;
            if (symbol == '_') return Tile.Type.Hole;

            if ("BRGYCPMX".Contains(symbol)) return Tile.Type.Hole;

            return Tile.Type.Normal;
        }

        private bool IsPositionInGrid(Point gridPosition)
        {
            return gridPosition.X >= 0 && gridPosition.X < GridWidth
                && gridPosition.Y >= 0 && gridPosition.Y < GridHeight;
        }
    }
}