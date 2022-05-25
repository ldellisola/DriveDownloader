using System.Text.RegularExpressions;
using DriveDownloader.Worker.CLI.Commands;

namespace DriveDownloader.Worker.CLI;

internal static class InteractivePrompt
{
        private static int _startingCursorLeft;
        private static int _startingCursorTop;
        private static ConsoleKeyInfo _key, _lastKey;
        private static string _prompt = "";
        private static bool _stop;
        private static readonly List<ICommand> Commands = new();

        public static void AddCommand<T>() where T : ICommand, new() => Commands.Add(new T());
        public static void AddCommands(params ICommand[] commands) => Commands.AddRange(commands);
        public static IReadOnlyList<ICommand> GetCommands() => Commands;
        public static void Exit() => _stop = true;
        private static bool InputIsOnNewLine(List<char> input, int inputPosition)
        {
            return (inputPosition + _prompt.Length > Console.BufferWidth - 1);
        }
        private static int GetCurrentLineForInput(List<char> input, int inputPosition)
        {
            int currentLine = 0;
            for (int i = 0; i < input.Count; i++)
            {
                if (input[i] == '\n')
                    currentLine += 1;
                if (i == inputPosition)
                    break;
            }
            return currentLine;
        }
        /// <summary>
        /// Gets the cursor position relative to the current line it is on
        /// </summary>
        /// <param name="input"></param>
        /// <param name="inputPosition"></param>
        /// <returns></returns>
        private static Tuple<int,int> GetCursorRelativePosition(List<char> input, int inputPosition)
        {
            int currentPos = 0;
            int currentLine = 0;
            for (int i = 0; i < input.Count; i++)
            {
                if (input[i] == '\n')
                {
                    currentLine += 1;
                    currentPos = 0;
                }
                if (i == inputPosition)
                {
                    if (currentLine == 0)
                    {
                        currentPos += _prompt.Length;
                    }
                    break;
                }
                currentPos++;
            }
            return Tuple.Create(currentPos, currentLine);
        }
        private static int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }
        private static void ClearLine(List<char> input, int inputPosition)
        {
            int cursorLeft = InputIsOnNewLine(input, inputPosition) ? 0 : _prompt.Length;
            Console.SetCursorPosition(cursorLeft, Console.CursorTop);
            Console.Write(new string(' ', input.Count + 5));
        }

        /// <summary>
        /// A hacktastic way to scroll the buffer - WriteLine
        /// </summary>
        /// <param name="lines"></param>
        private static void ScrollBuffer(int lines = 0)
        {
            for (int i = 0; i <= lines; i++)
                Console.WriteLine("");
            Console.SetCursorPosition(0, Console.CursorTop - lines);
            _startingCursorTop = Console.CursorTop - lines;
        }

        /// <summary>
        /// RewriteLine will rewrite every character in the input List, and given the inputPosition
        /// will determine whether or not to continue to the next line
        /// </summary>
        /// <param name="input">The input buffer</param>
        /// <param name="inputPosition">Current character position in the List</param>
        private static void RewriteLine(List<char> input, int inputPosition)
        {
            int cursorTop = 0;

            try
            {
                Console.SetCursorPosition(_startingCursorLeft, _startingCursorTop);
                var coords = GetCursorRelativePosition(input, inputPosition);
                cursorTop = _startingCursorTop;
                int cursorLeft = 0;

                if (GetCurrentLineForInput(input, inputPosition) == 0)
                {
                    cursorTop += (inputPosition + _prompt.Length) / Console.BufferWidth;
                    cursorLeft = inputPosition + _prompt.Length;
                }
                else
                {
                    cursorTop += coords.Item2;
                    cursorLeft = coords.Item1 - 1;
                }

                // if the new vertical cursor position is going to exceed the buffer height (i.e., we are
                // at the bottom of console) then we need to scroll the buffer however much we are about to exceed by
                if (cursorTop >= Console.BufferHeight)
                {
                    ScrollBuffer(cursorTop - Console.BufferHeight + 1);
                    RewriteLine(input, inputPosition);
                    return;
                }

                Console.Write(String.Concat(input));
                Console.SetCursorPosition(Mod(cursorLeft, Console.BufferWidth), cursorTop);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        private static IEnumerable<string> GetMatch(List<string> s, string input)
        {
            s.Add(input);
            int direction = (_key.Modifiers == ConsoleModifiers.Shift) ? -1 : 1;
            for (int i = -1; i < s.Count; )
            {
                direction = (_key.Modifiers == ConsoleModifiers.Shift) ? -1 : 1;
                i = Mod((i + direction), s.Count);

                if (Regex.IsMatch(s[i], ".*(?:" + input + ").*", RegexOptions.IgnoreCase))
                {
                    yield return s[i];
                }
            }
        }

        static Tuple<int, int> HandleMoveLeft(List<char> input, int inputPosition)
        {
            var coords = GetCursorRelativePosition(input, inputPosition);
            int cursorLeftPosition = coords.Item1;
            int cursorTopPosition = Console.CursorTop;

            if (GetCurrentLineForInput(input, inputPosition) == 0)
                cursorLeftPosition = (coords.Item1) % Console.BufferWidth ;

            if (Console.CursorLeft == 0)
                cursorTopPosition = Console.CursorTop - 1;
            
            return Tuple.Create(cursorLeftPosition, cursorTopPosition);
        }

        static Tuple<int, int> HandleMoveRight(List<char> input, int inputPosition)
        {
            var coords = GetCursorRelativePosition(input, inputPosition);
            int cursorLeftPosition = coords.Item1;
            int cursorTopPosition = Console.CursorTop;
            if (Console.CursorLeft + 1 >= Console.BufferWidth || input[inputPosition] == '\n')
            {
                cursorLeftPosition = 0;
                cursorTopPosition = Console.CursorTop + 1;
            }
            return Tuple.Create(cursorLeftPosition % Console.BufferWidth, cursorTopPosition);
        }

        private static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {   
            foreach (T item in source) { action(item); }
        }

        /// <summary>
        /// Run will start an interactive prompt
        /// </summary>
        /// <param name="prompt">The prompt for the interactive shell</param>
        /// <param name="startupMsg">Startup msg to display to user</param>
        public static void Run(string prompt, string? startupMsg = default)
        {
            _prompt = prompt;
            Console.WriteLine(startupMsg ?? string.Empty);
            List<List<char>> inputHistory = new List<List<char>>();
            IEnumerator<string>? wordIterator = null;
            List<string> completionList = Commands.Select(t => t.CommandText).ToList();

            while (!_stop)
            {
                string? completion = null;
                List<char> input = new List<char>();
                _startingCursorLeft = _prompt.Length;
                _startingCursorTop = Console.CursorTop;
                int inputPosition = 0;
                int inputHistoryPosition = inputHistory.Count;

                _key = _lastKey = new ConsoleKeyInfo();
                Console.Write(prompt);
                do
                {
                    _key = Console.ReadKey(true);
                    if (_key.Key == ConsoleKey.LeftArrow)
                    {
                        if (inputPosition > 0)
                        {
                            inputPosition--;
                            var pos = HandleMoveLeft(input, inputPosition);
                            Console.SetCursorPosition(pos.Item1, pos.Item2);
                        }
                    }
                    else if (_key.Key == ConsoleKey.RightArrow)
                    {
                        if (inputPosition < input.Count)
                        {
                            var pos = HandleMoveRight(input, inputPosition++);
                            Console.SetCursorPosition(pos.Item1, pos.Item2);
                        }
                    }

                    else if (_key.Key == ConsoleKey.Tab && completionList is {Count: > 0})
                    {
                        int tempPosition = inputPosition;
                        List<char> word = new List<char>();
                        while (tempPosition-- > 0 && !string.IsNullOrWhiteSpace(input[tempPosition].ToString()))
                            word.Insert(0, input[tempPosition]);

                        if (_lastKey.Key == ConsoleKey.Tab)
                        {
                            wordIterator.MoveNext();
                            if (completion != null)
                            {
                                ClearLine(input, inputPosition);
                                for (var i = 0; i < completion.Length; i++)
                                {
                                    input.RemoveAt(--inputPosition);
                                }
                                RewriteLine(input, inputPosition);
                            }
                            else
                            {
                                ClearLine(input, inputPosition);
                                for (var i = 0; i < string.Concat(word).Length; i++)
                                {
                                    input.RemoveAt(--inputPosition);
                                }
                                RewriteLine(input, inputPosition);
                            }
                        }
                        else
                        {
                            ClearLine(input, inputPosition);
                            for (var i = 0; i < string.Concat(word).Length; i++)
                            {
                                input.RemoveAt(--inputPosition);
                            }
                            RewriteLine(input, inputPosition);
                            wordIterator = GetMatch(completionList, string.Concat(word)).GetEnumerator();
                            while (wordIterator.Current == null)
                                wordIterator.MoveNext();
                        }

                        completion = wordIterator.Current;
                        ClearLine(input, inputPosition);
                        foreach (var c in completion.ToCharArray())
                        {
                            input.Insert(inputPosition++, c);
                        }
                        RewriteLine(input, inputPosition);

                    }
                    else if (_key.Key == ConsoleKey.Home || (_key.Key == ConsoleKey.H && _key.Modifiers == ConsoleModifiers.Control))
                    {
                        inputPosition = 0;
                        Console.SetCursorPosition(prompt.Length, _startingCursorTop);
                    }

                    else if (_key.Key == ConsoleKey.End || (_key.Key == ConsoleKey.E && _key.Modifiers == ConsoleModifiers.Control))
                    {
                        inputPosition = input.Count;
                        var cursorLeft = 0;
                        int cursorTop = _startingCursorTop;
                        if ((inputPosition + _prompt.Length) / Console.BufferWidth > 0)
                        {
                            cursorTop += (inputPosition + _prompt.Length) / Console.BufferWidth;
                            cursorLeft = (inputPosition + _prompt.Length) % Console.BufferWidth;
                        }
                        Console.SetCursorPosition(cursorLeft, cursorTop);
                    }

                    else if (_key.Key == ConsoleKey.Delete)
                    {
                        if (inputPosition < input.Count)
                        {
                            input.RemoveAt(inputPosition);
                            ClearLine(input, inputPosition);
                            RewriteLine(input, inputPosition);
                        }
                    }

                    else if (_key.Key == ConsoleKey.UpArrow)
                    {
                        if (inputHistoryPosition > 0)
                        {
                            inputHistoryPosition -= 1;
                            ClearLine(input, inputPosition);

                            // ToList() so we make a copy and don't use the reference in the list
                            input = inputHistory[inputHistoryPosition].ToList();
                            RewriteLine(input, input.Count);
                            inputPosition = input.Count;
                        }
                    }
                    else if (_key.Key == ConsoleKey.DownArrow)
                    {
                        if (inputHistoryPosition < inputHistory.Count - 1)
                        {
                            inputHistoryPosition += 1;
                            ClearLine(input, inputPosition);

                            // ToList() so we make a copy and don't use the reference in the list
                            input = inputHistory[inputHistoryPosition].ToList();
                            RewriteLine(input, input.Count);
                            inputPosition = input.Count;
                        }
                        else
                        {
                            inputHistoryPosition = inputHistory.Count;
                            ClearLine(input, inputPosition);
                            Console.SetCursorPosition(prompt.Length, Console.CursorTop);
                            input = new List<char>();
                            inputPosition = 0;
                        }
                    }
                    else if (_key.Key == ConsoleKey.Backspace)
                    {
                        if (inputPosition > 0)
                        {
                            inputPosition--;
                            input.RemoveAt(inputPosition);
                            ClearLine(input, inputPosition);
                            RewriteLine(input, inputPosition);
                        }
                    }

                    else if (_key.Key == ConsoleKey.Escape)
                    {
                        if (_lastKey.Key == ConsoleKey.Escape)
                            Environment.Exit(0);
                        else
                            Console.WriteLine("Press Escape again to exit.");
                    }

                    else if (_key.Key == ConsoleKey.Enter && (_key.Modifiers == ConsoleModifiers.Shift || _key.Modifiers == ConsoleModifiers.Alt))
                    {
                        input.Insert(inputPosition++, '\n');
                        RewriteLine(input, inputPosition);
                    }

                    // multiline paste event
                    else if (_key.Key == ConsoleKey.Enter && Console.KeyAvailable == true)
                    {
                        input.Insert(inputPosition++, '\n');
                        RewriteLine(input, inputPosition);
                    }

                    else if (_key.Key != ConsoleKey.Enter)
                    {

                        input.Insert(inputPosition++, _key.KeyChar);
                        RewriteLine(input, inputPosition);
                    }
                    
                    _lastKey = _key;
                } while (!(_key.Key == ConsoleKey.Enter && Console.KeyAvailable == false) 
                    // If Console.KeyAvailable = true then we have a multiline paste event
                    || (_key.Key == ConsoleKey.Enter && (_key.Modifiers == ConsoleModifiers.Shift || _key.Modifiers == ConsoleModifiers.Alt)));

                int newlines = (input.Count(a => a == '\n') > (input.Count / Console.BufferWidth))
                             ? input.Count(a => a == '\n')
                             : (input.Count / Console.BufferWidth);
                // Console.SetCursorPosition(prompt.Length, _startingCursorTop + newlines + 1);
                Enumerable.Range(0, newlines+1).ForEach(x => Console.WriteLine());
                // Console.SetCursorPosition(prompt.Length, Console.CursorTop);


                var cmd = string.Concat(input);
                if (String.IsNullOrWhiteSpace(cmd))
                    continue;

                if (!inputHistory.Contains(input))
                    inputHistory.Add(input);

                // Console.WriteLine();
                Console.WriteLine(HandleInteraction(cmd.Trim()));

            }
        }

        private static string HandleInteraction(string cmd)
        {
            var selectedCommand = GetCommands().FirstOrDefault(t=> t.CanExecute(cmd));

            return selectedCommand switch
            {
                null => "Invalid command",
                _ => selectedCommand.Execute(cmd)
            };
        }
}


