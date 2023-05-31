using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CSharp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await StartGame();
        }

        /*
         * Function asking user to set the playing field dimensions and initiating to start the game
         */
        static async Task StartGame()
        {
            Console.Clear();
            (int width, int height) playingFieldDimensions = getPlayingFieldDimensions();
            await playInLoop(true, playingFieldDimensions.width, playingFieldDimensions.height);
        }

        /*
         * Background task to monitor if user pressed some key 
         */
        static async Task<ConsoleKey> MonitorUserInput()
        {
            while(true)
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
                    switch (keyInfo.Key)
                    {
                        case ConsoleKey.RightArrow:
                        case ConsoleKey.LeftArrow:
                        case ConsoleKey.DownArrow:
                        case ConsoleKey.Spacebar:
                            return keyInfo.Key;

                    }
                }
                await Task.Delay(1);
            }
        }

        /*
         * Function with game logic - loops when condition argument is true
         * Shows one figure at a time 
         */
        private static async Task playInLoop(bool condition, int fieldWidth, int fieldHeight)
        {
            Console.CursorVisible = false; // to get clean console every loop iteration to rerender the playing field

            int[,] playingFieldMatrix = getPlayingFieldCarcass(fieldWidth, fieldHeight); 

            List<(int x, int y)> figureCoordinates = getRandomFigure(fieldWidth);

            Random rnd = new Random(); //to select random figure color from the list of possible colors
            ConsoleColor[] figureColors = getFiguresColors();
            int num = rnd.Next(figureColors.Length);
            ConsoleColor color = figureColors[num];

            Task<ConsoleKey> userInputTask = MonitorUserInput(); //background task to monitor if user pressed some key
            int score = 0; //user score depending on the number of filled rows

            while (condition)
            {

                Console.Clear(); //rerender the playing field with every loop iteration
                checkFilledLine(playingFieldMatrix);
                drawPlayingField(playingFieldMatrix);
                drawFigure(figureCoordinates, color);

                if (userInputTask.IsCompleted) //check the background task
                {
                    ConsoleKey key = await userInputTask;
                    switch (key)
                    {
                        case ConsoleKey.RightArrow:
                            if (!checkOverlap(getNextRightCoordinates(figureCoordinates), playingFieldMatrix))
                            {
                                figureCoordinates = moveCoordinates(figureCoordinates, "right");
                            }
                            break;
                        case ConsoleKey.LeftArrow:
                            if (!checkOverlap(getNextLeftCoordinates(figureCoordinates), playingFieldMatrix))
                            {
                                figureCoordinates = moveCoordinates(figureCoordinates, "left");
                            }
                            break;
                        case ConsoleKey.DownArrow: //get all the way down 
                            while (!checkOverlap(getNextDownCoordinates(figureCoordinates), playingFieldMatrix))
                            {
                                figureCoordinates = moveCoordinates(figureCoordinates, "down");
                            }
                            break;
                        case ConsoleKey.Spacebar:
                            Console.SetCursorPosition(0, fieldHeight + 5);
                            bool userChoice = getUserConfirmation("Are you sure you want to restart? Please enter Y/N to try again/exit: ");
                            if (userChoice) await StartGame();
                            break;
                    }
                    userInputTask = MonitorUserInput();
                }

                List<(int,int)> coorTest = getNextDownCoordinates(figureCoordinates);
                bool overlap = checkOverlap(coorTest, playingFieldMatrix);

                if (!overlap)
                {
                    figureCoordinates = moveCoordinates(figureCoordinates, "down");
                }
                else
                {
                    foreach ((int x, int y) coordinate in figureCoordinates)
                    {
                        if (coordinate.y < 1)
                        {
                            condition = false;
                            GameOver(score);

                        }
                        if (coordinate.y > 0) playingFieldMatrix[coordinate.x, coordinate.y] = num + 2;
                    }

                    figureCoordinates = getRandomFigure(fieldWidth);
                    num = rnd.Next(figureColors.Length);
                    color = figureColors[num];
                }
                int filledLine = checkFilledLine(playingFieldMatrix);
                if (filledLine >0)
                {
                    score++;
                    playingFieldMatrix = excludeFilledLine(playingFieldMatrix, filledLine);
                }
                Console.SetCursorPosition(0, fieldHeight + 3);
                Console.WriteLine($"Your score is {score}");
                Console.WriteLine("Press spacebar to restart");
                await Task.Delay(1000 - 10*score);
            }
        }

        private static (int, int) getPlayingFieldDimensions()
        {
            bool dimensionsSet = false;
            int width = 0;
            int height = 0;
            while (!dimensionsSet)
            {
                Console.Write("Please enter the width of the playing field: ");
                width = getUserInputInt();
                Console.Write("Please enter the height of the playing field: ");
                height = getUserInputInt();
                Console.WriteLine("With given dimensions the playing field will look like this: ");
                drawPlayingField(getPlayingFieldCarcass(width, height));
                dimensionsSet = getUserConfirmation("Press Y/N to set/change dimensions: ");
            }
            return (width, height);
        }

        private static int getUserInputInt()
        {
            string errorMessage = "Only integers between 10 and 50 are accepted. Please try again";
            int count = 0;
            int intValue = 10;
            bool isInputInt = false;
            int cursorX = Console.CursorLeft;
            int cursorY = Console.CursorTop;
            while (!isInputInt|| intValue < 10 || intValue>50)
            {
                if (count > 0)
                {
                    Console.SetCursorPosition(cursorX, cursorY);
                    showTempErrorMessage(errorMessage, cursorX, cursorY);
                }
                string userInput = Console.ReadLine();
                isInputInt = int.TryParse(userInput, out intValue);
                count++;
            }
            return intValue;
        }

        private static void showTempErrorMessage(string message, int cursorX, int cursorY)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(message);
            Thread.Sleep(2000);
            Console.ResetColor();
            Console.SetCursorPosition(cursorX, cursorY);
            Console.Write(new string(' ', Console.WindowWidth - cursorX));
            Console.SetCursorPosition(cursorX, cursorY);
        }

        /*
         * Function that returns matrix of the empty playing field 
         */
        private static int[,] getPlayingFieldCarcass(int fieldWidth, int fieldHeight)
        {
            int[,] playFieldCoors = new int[(fieldWidth + 2) * 2, fieldHeight + 2];

            for (int y = 0; y < fieldHeight + 2; y++)
            {
                for (int x = 0; x < (fieldWidth + 2) * 2; x++)
                {
                    if (x > (fieldWidth + 1) * 2)
                    {
                        playFieldCoors[x, y] = 1;
                    }
                    else if (x < 1 * 2 || (x == (fieldWidth + 1) * 2))
                    {
                        playFieldCoors[x, y] = 1;
                    }

                    else if (y == 0 || y == fieldHeight + 1)
                    {
                        playFieldCoors[x, y] = 1;
                    }
                    else
                    {
                        playFieldCoors[x, y] = 0;
                    }
                }
            }
            return playFieldCoors;
        }

        private static void drawPlayingField(int[,] fieldValuesRepresentation)
        {
            for (int y = 0; y < fieldValuesRepresentation.GetLength(1); y++)
                for (int x = 0; x < fieldValuesRepresentation.GetLength(0); x++)
                {
                    int fieldValue = fieldValuesRepresentation[x, y];
                    switch (fieldValue)
                    {
                        case 0:
                            Console.BackgroundColor = ConsoleColor.White;
                            break;
                        case 1:
                            Console.BackgroundColor = ConsoleColor.Black;
                            break;
                        default: //other colors
                            Console.BackgroundColor = getFiguresColors()[fieldValue-2];
                            break;
                    }
                    if (x == fieldValuesRepresentation.GetLength(0)-1)
                    {
                        Console.WriteLine(" ");
                    } else
                    {
                        Console.Write(" ");
                    }
                    Console.ResetColor();
                }
        }

        private static int checkFilledLine(int[,] fieldMatrix)
        {
            int lineExclude = -1;
            for (int y = fieldMatrix.GetLength(1) - 2; y > 0; y--) //not counting the borders
            {
                for (int x = 2; x < fieldMatrix.GetLength(0) - 2; x +=2) //because we take double place by x 
                {
                    if (fieldMatrix[x, y] == 0) break;
                    if (x == fieldMatrix.GetLength(0) - 4)
                    {
                        lineExclude = y;
                    }
                }
            }
            return lineExclude;
        }

        private static int[,] excludeFilledLine(int[,] fieldMatrix, int lineExclude)
        {
            int[,] newFieldMatrix = new int[fieldMatrix.GetLength(0), fieldMatrix.GetLength(1)];
            for (int x = 0; x < fieldMatrix.GetLength(0); x++)
            {
                if (x == 0 || x == 1 || x == fieldMatrix.GetLength(0) - 1 || x == fieldMatrix.GetLength(0) - 2)
                {
                    newFieldMatrix[x, 1] = 1;
                }
                else
                {
                    newFieldMatrix[x, 1] = 0;
                }

            }
            for (int y = fieldMatrix.GetLength(1) - 1; y >= 0; y--)
            {
                int changedY;
                if (y > lineExclude || y == 0)
                {
                    changedY = y;
                }
                else if (y == lineExclude)
                {
                    continue;
                }
                else
                {
                    changedY = y + 1;
                }
                for (int x = 0; x < fieldMatrix.GetLength(0); x++)
                {
                    newFieldMatrix[x, changedY] = fieldMatrix[x, y];
                }
            }
            return newFieldMatrix;
        }

        private static bool getUserConfirmation(string message)
        {
            Console.Write(message);
            var userChoice = Console.ReadLine();
            switch (userChoice)
            {
                case "Y":
                case "y":
                    return true;
                case "N":
                case "n":
                    return false;
                default:
                    Console.Write("Incorrect selection. ");
                    return getUserConfirmation(message);
            }
        }

        /*
         * Function that returns coordinates for small line (with the beginning at (0,0) - vertical and horizontal
         */
        private static List<(int, int)> getSmallLineCoordinates(int xPos, int yPos, bool horizontal)
        {
            List<(int, int)> coordinates = new List<(int, int)>(); 
            if (horizontal)
            {
                for (int i = 0; i < 4; i++) {
                    coordinates.Add((xPos +i, yPos));
                }
            } else
            {
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        coordinates.Add((xPos + i, yPos + j));
                    }
                }
            }
            return coordinates;
        }

        // Function that returns coordinates for small line - vertical and horizontal
        private static List<(int, int)> getLongLineCoordinates(int xPos, int yPos, bool horizontal)
        {
            List<(int, int)> coordinates = new List<(int, int)>();
            if (horizontal)
            {
                for (int i = 0; i < 8; i++)
                {
                    coordinates.Add((xPos + i, yPos));
                }
            }
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        coordinates.Add((xPos + i, yPos - j));
                    }
                }
            }
            return coordinates;
        }

        // Function that returns coordinates for square
        private static List<(int, int)> getSquareCoordinates(int xPos, int yPos)
        {
            List<(int, int)> coordinates = new List<(int, int)>();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    coordinates.Add((xPos + i, yPos + j));
                }
            }
            return coordinates;
        }

        /*
         * Function that returns coordinates for dot figure (with the beginning at (0,0)
         */
        private static List<(int, int)> getDotCoordinates(int xPos, int yPos)
        {
            List<(int, int)> coordinates = new List<(int, int)>() { (xPos, yPos), (xPos + 1, yPos)};

            return coordinates;
        }

        private static List<(int, int)> getTriangleCoordinates(int xPos, int yPos, bool horizontal, bool upOrLeft)
        {
            List<(int, int)> coordinates = new List<(int, int)>();
            if (horizontal)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (i == 2 || i == 3)
                    {
                        if (upOrLeft) coordinates.Add((xPos + i, yPos - 1));
                        else coordinates.Add((xPos + i, yPos));
                    }
                    if (upOrLeft) coordinates.Add((xPos + i, yPos));
                    else coordinates.Add((xPos + i, yPos - 1));
                    
                }
            } else
            {
                for (int i = 0; i < 3; i++)
                {
                    if (i == 1)
                    {
                        if (upOrLeft)
                        {
                            coordinates.Add((xPos - 1, yPos - i));
                            coordinates.Add((xPos - 2, yPos - i));
                        }
                        else {
                            coordinates.Add((xPos + 2, yPos - i));
                            coordinates.Add((xPos + 3, yPos - i));
                        };
                    }
                    coordinates.Add((xPos, yPos - i));
                    coordinates.Add((xPos +1, yPos - i));
                }
            }
            return coordinates;
        }



        private static void drawFigure(List<(int x, int y)> coordinates, ConsoleColor color)
        {
            Console.BackgroundColor = color;
            foreach ((int x, int y) coordinate in coordinates)
            {
                if (coordinate.y > 0)
                {
                    Console.SetCursorPosition(coordinate.x, coordinate.y);
                    Console.Write(" ");
                }
            }
            Console.ResetColor();
        }

        /*
         * getNextDownCoordinates, getNextLeftCoordinates and getNextRightCoordinates are set of functions 
         * that return the changed figure's coordinates based on the direction of its next move (down, left, right)
         */
        private static List<(int, int)> getNextDownCoordinates(List<(int, int)> coordinates)
        {
            List<(int, int)> newCoords = new List<(int, int)>();
            foreach ((int x, int y) coordinate in coordinates)
            {
                newCoords.Add((coordinate.x, coordinate.y + 1));
            }
            return newCoords;
        }

        private static List<(int, int)> getNextLeftCoordinates(List<(int, int)> coordinates)
        {
            List<(int, int)> newCoords = new List<(int, int)>();
            foreach ((int x, int y) coordinate in coordinates)
            {
                newCoords.Add((coordinate.x -2, coordinate.y));
            }
            return newCoords;
        }

        private static List<(int, int)> getNextRightCoordinates(List<(int, int)> coordinates)
        {
            List<(int, int)> newCoords = new List<(int, int)>();
            foreach ((int x, int y) coordinate in coordinates)
            {
                newCoords.Add((coordinate.x +2, coordinate.y));
            }
            return newCoords;
        }

        private static bool checkOverlap(List<(int, int)> figureCoordinates, int[,] fieldMatrix)
        {
            foreach ((int x, int y) coordinate in figureCoordinates)
            {
                if (coordinate.y > 0 && fieldMatrix[coordinate.x, coordinate.y] != 0) {
                    return true;
                }
            }
            return false;
        }

        private static List<(int, int)> moveCoordinates(List<(int, int)> coordinates, string direction)
        {
            List<(int, int)> newCoordinates = new List<(int, int)>();
            foreach ((int x, int y) coordinate in coordinates)
            {
                switch (direction)
                {
                    case "left":
                        newCoordinates.Add((coordinate.x - 2, coordinate.y));
                        break;
                    case "right":
                        newCoordinates.Add((coordinate.x + 2, coordinate.y));
                        break;
                    case "down":
                        newCoordinates.Add((coordinate.x, coordinate.y + 1));
                        break;
                }

            }
            return newCoordinates;
        }

        /*
         * Function that returns array of possible colors for playing figures
         */
        private static ConsoleColor[] getFiguresColors()
        {
            ConsoleColor[] consoleColors = (ConsoleColor[])Enum.GetValues(typeof(ConsoleColor));
            return Array.FindAll(consoleColors, c => c != ConsoleColor.Black && c != ConsoleColor.White && c != ConsoleColor.Gray && c != ConsoleColor.DarkGray);
        }

        /*
         * Function that returns list of tuples representing fugure coordinates
         */
        private static List<(int,int)> getRandomFigure(int width)
        {
            Random rnd = new Random();
            Figure[] figures = (Figure[])Enum.GetValues(typeof(Figure));
            List<(int, int)> figureCoords = new List<(int, int)>();
            Figure figure = figures[rnd.Next(figures.Length)];
            switch (figure)
            {
                case Figure.SmallLineHorizontal:
                    figureCoords = getSmallLineCoordinates(rnd.Next(1, (width - 1)) * 2, 1, true);
                    break;
                case Figure.SmallLineVertical:
                    figureCoords = getSmallLineCoordinates(rnd.Next(1, (width - 1)) * 2, 1, false);
                    break;
                case Figure.LongLineHorizontal:
                    figureCoords = getLongLineCoordinates(rnd.Next(1, (width - 2)) * 2, 1, true);
                    break;
                case Figure.LongLineVertical:
                    figureCoords = getLongLineCoordinates(rnd.Next(1, (width - 2)) * 2, 1, false);
                    break;
                case Figure.Square:
                    figureCoords = getSquareCoordinates(rnd.Next(1, (width - 1)) * 2, 1);
                    break;
                case Figure.Dot:
                    figureCoords = getDotCoordinates(rnd.Next(1, (width - 1)) * 2, 1);
                    break;
                case Figure.TriangleUp:
                    figureCoords = getTriangleCoordinates(rnd.Next(1, (width - 1)) * 2, 1, true, true);
                    break;
                case Figure.TriangleDown:
                    figureCoords = getTriangleCoordinates(rnd.Next(1, (width - 1)) * 2, 1, true, false);
                    break;
                case Figure.TriangleLeft:
                    figureCoords = getTriangleCoordinates(rnd.Next(2, (width - 1)) * 2, 1, false, true);
                    break;
                case Figure.TriangleRight:
                    figureCoords = getTriangleCoordinates(rnd.Next(2, (width - 1)) * 2, 1, false, false);
                    break;
            }
            return figureCoords;
        }

        private enum Figure
        {
            SmallLineHorizontal,
            SmallLineVertical,
            LongLineHorizontal,
            LongLineVertical,
            Square,
            Dot,
            TriangleUp,
            TriangleDown,
            TriangleLeft,
            TriangleRight
        }

        private static async void GameOver(int score)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"GAME OVER\nYour score is {score}");
            Console.ResetColor();
            bool userChoice = getUserConfirmation("Do you want to try again? Please enter Y/N to try again/exit: ");
            if (userChoice) await StartGame();
            else
            {
                Environment.Exit(0);
            }
        }
    }
}