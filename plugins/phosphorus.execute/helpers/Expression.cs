/*
 * phosphorus five, copyright 2014 - Mother Earth, Jannah, Gaia
 * phosphorus five is licensed as mit, see the enclosed LICENSE file for details
 */

using System;
using System.Globalization;
using System.Collections.Generic;
using phosphorus.core;

namespace phosphorus.execute
{
    /// <summary>
    /// expression class, for retrieving and changing values in node tree according to execution expressions
    /// </summary>
    public class Expression
    {
        private List<string> _tokens;
        private string _expression;

        /// <summary>
        /// initializes a new instance of the <see cref="phosphorus.execute.Expression"/> class
        /// </summary>
        /// <param name="expression">execution engine expression</param>
        public Expression (string expression)
        {
            if (string.IsNullOrEmpty(expression) || !expression.StartsWith ("@"))
                throw new ArgumentException (string.Format ("'{0}' is not a valid expression", expression));
            _expression = expression.Substring (1);

            _tokens = TokenizeExpression (_expression);
            if (_tokens.Count == 0)
                throw new ArgumentException (string.Format ("'{0}' is not a valid expression", expression));
        }

        /// <summary>
        /// determines if string is an expression or not
        /// </summary>
        /// <returns><c>true</c> if string is an expression; otherwise, <c>false</c>.</returns>
        /// <param name="value">string to check</param>
        public static bool IsExpression (string value)
        {
            return value.StartsWith ("@") && !value.StartsWith (@"@""");
        }

        /// <summary>
        /// returns formatted node according to children nodes
        /// </summary>
        /// <returns>the formatted string expression</returns>
        /// <param name="node">node to format</param>
        public static string FormatNode (Node node)
        {
            string retVal = node.Get<string> ();
            if (node.Count > 0) {
                string[] childrenValues = new string[node.Count];
                int idxNo = 0;
                foreach (Node idxNode in node.Children) {
                    string value = idxNode.Get<string> ();
                    if (idxNode.Count > 0)
                        value = FormatNode (idxNode);
                    if (IsExpression (value)) {
                        var match = new Expression (value).Evaluate (idxNode);
                        if (match.Count > 1)
                            throw new ArgumentException ("expression in format node returned more than one match");
                        else if (match.Count == 0)
                            value = null;
                        else {
                            object obj = match.GetValue (0);
                            if (obj != null)
                                value = obj.ToString ();
                            else
                                value = null;
                        }
                    }
                    childrenValues [idxNo++] = value;
                }
                retVal = string.Format (CultureInfo.InvariantCulture, retVal, childrenValues);
            }
            return retVal;
        }

        /// <summary>
        /// evaluates expression for given <see cref="phosphorus.core.Node"/>  and returns <see cref="phosphorus.execute.Expression.Match"/> 
        /// </summary>
        public Match Evaluate (Node node)
        {
            MatchIterator currentIterator = MatchIterator.Create (node);
            List<string> previousTokens = new List<string> ();
            for (int idxNo = 0; idxNo < _tokens.Count - 1; idxNo ++) {
                previousTokens.Add (_tokens [idxNo]);
                currentIterator = FindMatches (currentIterator, previousTokens);
                if (!currentIterator.HasMatch)
                    return null;
            }
            switch (_tokens [_tokens.Count - 1]) {
                case "name":
                    return new Match (currentIterator, Match.MatchType.Name);
                case "value":
                   return new Match (currentIterator, Match.MatchType.Value);
                case "path":
                    return new Match (currentIterator, Match.MatchType.Path);
                case @"\":
                    return new Match (currentIterator, Match.MatchType.Node);
                case "/":
                    return new Match (currentIterator, Match.MatchType.Children);
                default:
                    throw new ArgumentException ("that is not a valid expression, don't know how to return; '" + _tokens [_tokens.Count - 1] + "'");
            }
        }
        
        /*
         * return matches according to token
         */
        private MatchIterator FindMatches (MatchIterator lastMatches, List<string> previousTokens)
        {
            string token = previousTokens [previousTokens.Count - 1];
            switch (token) {
                case "/":
                    if (previousTokens.Count == 1) {
                        // returning root since "/" is found as first token of expression
                        return new MatchIteratorRoot (lastMatches);
                    } else if (previousTokens [previousTokens.Count - 2] == "/") {
                        // returning all nodes with empty names since two / tokens have followed each other
                        return new MatchIteratorNamedNode (lastMatches, "");
                    } else {
                        // token is simply here to separate one token from the next, hence we return simply last match
                        return lastMatches;
                    }
                case "*":
                    return new MatchIteratorAllChildren (lastMatches);
                case "**":
                    return new MatchIteratorAllDescendants (lastMatches);
                case ".":
                    return new MatchIteratorAllParents (lastMatches);
                default:
                    return new MatchIteratorNamedNode (lastMatches, token);
            }
        }

        /*
         * responsible for tokenizing expression
         */
        private static List<string> TokenizeExpression (string expression)
        {
            List<string> tokens = new List<string> ();
            string buffer = string.Empty;
            for (int idxNo = 0; idxNo < expression.Length; idxNo++) {
                char idxChar = expression [idxNo];
                switch (idxChar) {
                    case '/':
                    case '\\':
                    case '.':
                        if (buffer != string.Empty) {
                            tokens.Add (buffer);
                            buffer = string.Empty;
                        }
                        tokens.Add (idxChar.ToString ());
                        break;
                    case '@':
                        if (buffer != string.Empty) {
                            tokens.Add (buffer);
                            buffer = string.Empty;
                        }
                        tokens.Add (ReadStringLiteral (expression, ref idxNo));
                        break;
                    default:
                        buffer += idxChar;
                        break;
                }
            }
            if (buffer != string.Empty)
                tokens.Add (buffer);
            return tokens;
        }
        
        /*
         * reads string literal during tokenization process
         */
        private static string ReadStringLiteral (string expression, ref int idxNo)
        {
            string buffer = "";
            idxNo += 2; // skipping @" parts, and looping until end of string literal
            while (true) {
                char idxChar = expression [idxNo];
                if (buffer.Length > 0 && idxChar != '"' && (buffer.Length - buffer.TrimEnd (new char[] { '"' }).Length) % 2 == 1)
                    break;
                buffer += idxChar.ToString ();
                idxNo += 1;
            }
            --idxNo;
            return buffer.Substring (0, buffer.Length - 1).Replace (@"""""", @"""");
        }
    }
}

