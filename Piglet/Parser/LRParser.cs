﻿using System.Collections.Generic;
using Piglet.Lexer;
using Piglet.Parser.Construction;

namespace Piglet.Parser
{
    public class LRParser<T> : IParser<T>
    {
        private readonly IParseTable<T> parseTable;
        private readonly Stack<T> valueStack;
        private readonly Stack<int> parseStack;

        public LRParser(IParseTable<T> parseTable)
        {
            this.parseTable = parseTable;
            valueStack = new Stack<T>();
            parseStack = new Stack<int>();
        }

        public T Parse(ILexer<T> lexer)
        {
            // If this parser has been used before, clear the stacks
            valueStack.Clear();
            parseStack.Clear();

            // Push default state onto the parse stack. Default state is always 0
            parseStack.Push(0);

            var input = lexer.Next();

            while (true)
            {
                int state = parseStack.Peek();
                int action = parseTable.Action[state, input.Item1];
                if (action >= 0)
                {
                    if (action == int.MaxValue)
                    {
                        // Accept!
                        return valueStack.Pop();
                    }

                    // Shift
                    parseStack.Push(input.Item1);   // Push token unto stack
                    parseStack.Push(action);        // Push state unto stack

                    // Shift token value unto value stack
                    valueStack.Push(input.Item2);

                    // Lex next token
                    input = lexer.Next();
                }
                else
                {
                    if (action == int.MinValue)
                    {
                        throw new ParseException("Illegal token");
                    }

                    // Get the right reduction rule to apply
                    ReductionRule<T> reductionRule = parseTable.ReductionRules[-(action + 1)];
                    for (int i = 0; i < reductionRule.NumTokensToPop * 2; ++i)
                    {
                        parseStack.Pop();
                    }

                    // Transfer to state found in goto table
                    int stateOnTopOfStack = parseStack.Peek();
                    parseStack.Push(reductionRule.TokenToPush);
                    parseStack.Push(parseTable.Goto[stateOnTopOfStack, reductionRule.TokenToPush]);

                    // Get tokens off the value stack for the OnReduce function to run on
                    var onReduceParams = new T[reductionRule.NumTokensToPop];

                    // Need to do it in reverse since thats how the stack is organized
                    for (int i = reductionRule.NumTokensToPop - 1; i >= 0; --i)
                    {
                        onReduceParams[i] = valueStack.Pop();
                    }
                    valueStack.Push(reductionRule.OnReduce(onReduceParams));
                }
            }
        }
    }
}