
class Program
{
    public class State
    {
        public int Id { get; set; } = -1;
        public Dictionary<char, List<State>> Transitions;
        public bool final { get; set; } = false;

        public State()
        {
            Transitions = new Dictionary<char, List<State>>();
        }

        public void AddTransition(char symbol, State state)
        {
            if (!Transitions.ContainsKey(symbol))
            {
                Transitions[symbol] = new List<State>();
            }
            Transitions[symbol].Add(state);
        }
    }

    public class NFA
    {
        public State StartState;
        public State EndState;

        public NFA(State start, State end)
        {
            StartState = start;
            EndState = end;
        }
    }

    public class NFAConstructor
    {
        private int _stateCounter = 0;
        private int _currentStateId = 0;
        public List<bool> outputFinalStates = new();
        private State CreateState()
        {
            _stateCounter++;
            return new State();
        }

        public int GetStateCount()
        {
            return _stateCounter;
        }

        public NFA CreateSymbolNFA(char symbol)
        {
            if (symbol == 'e')
            {
                symbol = 'ε';
            }
            State start = CreateState();
            State end = CreateState();
            end.final = true;
            start.AddTransition(symbol, end);
            return new NFA(start, end);
        }

        public NFA Concatenate(NFA first, NFA second)
        {
            first.EndState.AddTransition('ε', second.StartState);
            first.EndState.final = false;
            return new NFA(first.StartState, second.EndState);
        }

        public NFA Union(NFA first, NFA second)
        {
            State start = CreateState();
            State end = CreateState();
            end.final = true;
            second.EndState.final = false;
            first.EndState.final = false;
            start.AddTransition('ε', first.StartState);
            start.AddTransition('ε', second.StartState);
            first.EndState.AddTransition('ε', end);
            second.EndState.AddTransition('ε', end);
            return new NFA(start, end);
        }

        public NFA Star(NFA nfa)
        {
            State start = CreateState();
            State end = CreateState();
            end.final = true;
            nfa.EndState.final = false;
            start.AddTransition('ε', nfa.StartState);
            start.AddTransition('ε', end);
            nfa.EndState.AddTransition('ε', nfa.StartState);
            nfa.EndState.AddTransition('ε', end);
            return new NFA(start, end);
        }

        public NFA Plus(NFA nfa)
        {
            State start = CreateState();
            State end = CreateState();
            end.final = true;
            nfa.EndState.final = false;
            start.AddTransition('ε', nfa.StartState);
            nfa.EndState.AddTransition('ε', nfa.StartState);
            nfa.EndState.AddTransition('ε', end);
            return new NFA(start, end);
        }

        public NFA BuildNFA(string regex)
        {
            Stack<NFA> stack = new Stack<NFA>();

            Stack<char> operators = new Stack<char>();

            Dictionary<char, int> precedence = new Dictionary<char, int>
            {
                { '|', 1 },
                { '*', 2 },
                { '+', 3 }
            };

            for (int i = 0; i < regex.Length; i++)
            {
                char c = regex[i];
 
                if (c == '(')
                {
                    if (i - 1 >= 0 && (regex[i - 1] != '(') && (regex[i - 1] != '|'))
                    {
                        operators.Push('.');
                    }
                    operators.Push('(');
                }
                else if (c == ')')
                {
                    while (operators.Peek() != '(')
                    {
                        OperatorsHandler(stack, operators);
                    }
                    operators.Pop();
                }
                else if (precedence.ContainsKey(c))
                {
                    operators.Push(c);
                    if (c == '*' || c == '+')
                    {
                        OperatorsHandler(stack, operators);
                    }
                } 
                else
                {
                    stack.Push(CreateSymbolNFA(c));

                    if (i - 1 >= 0 && (regex[i - 1] != '(') && (regex[i - 1] != '|'))
                    {
                        operators.Push('.');
                    }
                }
            }

            while (operators.Count != 0)
            {
                OperatorsHandler(stack, operators);
            }

            return stack.Pop();
        }

        private void OperatorsHandler(Stack<NFA> stack, Stack<char> operators)
        {
            var head = operators.Pop();
            if (head == '.')
            {
                var second = stack.Pop();
                var first = stack.Pop();
                stack.Push(Concatenate(first, second));
            }
            else if (head == '|')
            {
                var second = stack.Pop();
                var first = stack.Pop();
                stack.Push(Union(first, second));
            }
            else if (head == '*')
            {
                var nfa = stack.Pop();
                stack.Push(Star(nfa));
            }
            else if (head == '+')
            {
                var nfa = stack.Pop();
                stack.Push(Plus(nfa));
            }
        }

        public Dictionary<char, List<string>> ConvertToTransitionList(NFA nfa)
        {
            Dictionary<char, List<string>> transitionsList = new();
            HashSet<int> visited = new();

            for (int i = 0; i < _stateCounter; i++)
            {
                outputFinalStates.Add(false);
            }

            nfa.StartState.Id = _currentStateId;

            TraverseState(nfa.StartState, visited, transitionsList);

            return transitionsList;
        }

        private void TraverseState(State state, HashSet<int> visited, Dictionary<char, List<string>> transitionsList)
        {
            if (visited.Contains(state.Id))
            {
                return;
            }
            
            visited.Add(state.Id);
            outputFinalStates[state.Id] = state.final;
            foreach (var transition in state.Transitions)
            {
                char symbol = transition.Key;
                foreach (var nextState in transition.Value)
                {
                    if (nextState.Id == -1)
                    {
                        _currentStateId++;
                        nextState.Id = _currentStateId;
                    }
                    if (!transitionsList.ContainsKey(symbol))
                    {
                        transitionsList.Add(symbol, new List<string>(_stateCounter));
                        for (int i = 0; i < _stateCounter; i++)
                        {
                            transitionsList[symbol].Add(String.Empty);
                        }
                    }
                    if (transitionsList[symbol][state.Id] == String.Empty)
                    {
                        transitionsList[symbol][state.Id] = 'q' + nextState.Id.ToString();
                    } else
                    {
                        transitionsList[symbol][state.Id] += ",q" + nextState.Id.ToString();
                    }
                }
            }

            foreach (var transition in state.Transitions)
            {
                foreach (var nextState in transition.Value)
                {
                    TraverseState(nextState, visited, transitionsList);
                }
            }
        }
    }

    public class RegexParser
    {
        public string ToPostfix(string regex)
        {
            Stack<char> operators = new Stack<char>();
            string postfix = "";

            Dictionary<char, int> precedence = new Dictionary<char, int>
            {
                { '|', 1 },
                { '.', 2 },
                { '*', 3 },
                { '+', 3 }
            };

            for (int i = 0; i < regex.Length; i++)
            {
                char c = regex[i];

                if (char.IsLetterOrDigit(c))
                {
                    postfix += c;

                    if (i + 1 < regex.Length && (char.IsLetterOrDigit(regex[i + 1]) || regex[i + 1] == '('))
                    {
                        operators.Push('.');
                    }
                }
                else if (c == '(')
                {
                    operators.Push(c);
                }
                else if (c == ')')
                {
                    while (operators.Peek() != '(')
                    {
                        postfix += operators.Pop();
                    }
                    operators.Pop();
                }
                else
                {
                    while (operators.Count > 0 && operators.Peek() != '(' && precedence[c] <= precedence[operators.Peek()])
                    {
                        postfix += operators.Pop();
                    }
                    operators.Push(c);
                }
            }

            
            while (operators.Count > 0)
            {
                postfix += operators.Pop();
            }

            return postfix;
        }
    }

    static void Main(string[] args)
    {
        string inputPath = args[1];
        string outputPath = args[2];

        var lines = File.ReadLines(inputPath);
        string regex = lines.First();

        var constructor = new NFAConstructor();
        NFA nfa = constructor.BuildNFA(regex);

        Dictionary<char, List<string>> transitionsList = constructor.ConvertToTransitionList(nfa);

        using (var writer = new StreamWriter(outputPath))
        {
            for (int i = 0; i < constructor.GetStateCount(); i++)
            {
                if (constructor.outputFinalStates[i])
                {
                    writer.Write(";F");
                }
                else
                {
                    writer.Write(";");
                }
            }

            writer.WriteLine();

            for (int i = 0; i < constructor.GetStateCount(); i++)
            {
                writer.Write(";q" + i);
            }

            writer.WriteLine();

            foreach (var transitions in transitionsList)
            {
                writer.Write(transitions.Key.ToString() + ';');
                foreach (var transition in transitions.Value)
                {
                    writer.Write(transition + ';');
                }
                writer.WriteLine();
            }
        }
    }
}