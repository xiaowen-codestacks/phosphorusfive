
/*
 * phosphorus five, copyright 2014 - Mother Earth, Jannah, Gaia
 * phosphorus five is licensed as mit, see the enclosed LICENSE file for details
 */

using System;
using System.Globalization;
using System.Collections.Generic;
using phosphorus.core;

namespace phosphorus.expressions
{
    /// <summary>
    /// contains useful helper methods for dealing with pf.lambda expressions
    /// </summary>
    public static class XUtil
    {
        /// <summary>
        /// delegate used when iterating expressions
        /// </summary>
        public delegate void IteratorCallback<T> (T idx);

        /// <summary>
        /// returns true if value is an expression
        /// </summary>
        /// <returns><c>true</c> if value is an expression; otherwise, <c>false</c></returns>
        /// <param name="value">value to check</param>
        public static bool IsExpression (object value)
        {
            if (value == null)
                return false;
            return IsExpression (value as string);
        }

        /// <summary>
        /// returns true if value is an expression
        /// </summary>
        /// <returns><c>true</c> if value is an expression; otherwise, <c>false</c></returns>
        /// <param name="value">value to check</param>
        public static bool IsExpression (string value)
        {
            return value != null && 
                value.StartsWith ("@") && 
                value.Length >= 4; // "@{0}" is the shortest possible expression, and has 4 characters
        }

        /// <summary>
        /// returns true if given node contains formatting parameters
        /// </summary>
        /// <returns><c>true</c> if node contains formatting parameters; otherwise, <c>false</c></returns>
        /// <param name="node">node to check</param>
        public static bool IsFormatted (Node node)
        {
            // a formatted node is defined as having one or more children with string.Empty as name
            // and a value which is of type string
            return node.Value is string && node.FindAll (string.Empty).GetEnumerator ().MoveNext ();
        }
        
        /// <summary>
        /// formats given node and returns formatted node value as string
        /// </summary>
        /// <returns>formatter string</returns>
        /// <param name="node">node containing formatting expression</param>
        /// <param name="context">application context</param>
        public static string FormatNode (Node node, ApplicationContext context)
        {
            return FormatNode (node, node, context);
        }

        /// <summary>
        /// formats given node and returns formatted node value as string
        /// </summary>
        /// <returns>the formatted value of node</returns>
        /// <param name="node">node to format</param>
        /// <param name="dataSource">data source to use as root for expressions within formatting values</param>
        /// <param name="context">application context</param>
        public static string FormatNode (Node node, Node dataSource, ApplicationContext context)
        {
            // making sure node contains formatting values
            if (!IsFormatted (node))
                throw new ArgumentException ("cannot format node, no formatting nodes exists, or node's value is not a string");

            // retrieving all "formatting values"
            List<string> childrenValues = new List<string> (node.ConvertChildren<string> (
            delegate (Node idx) {

                // we only use nodes who's names are empty as "formatting nodes"
                if (idx.Name == string.Empty) {

                    // recursively format and evaluate expressions of children nodes
                    return FormatNodeRecursively (idx, dataSource, context);
                } else {

                    // this is not a part of the formatting values for our formating expression
                    return null;
                }
            }));

            // returning node's value, after being formatted, according to its children node's values
            // PS, at this point all childrenValues have already been converted by the engine itself to string values
            return string.Format (CultureInfo.InvariantCulture, node.Get<string> (context), childrenValues.ToArray ());
        }

        /// <summary>
        /// returns a single value from the given node. if node has an expression as its value, it
        /// will iterate the results of that expression, concatenating the results back to one value, before
        /// converting to T. if node contains anything but an expression, it will return that object as type T
        /// back to caller
        /// </summary>
        /// <param name="node">node</param>
        /// <param name="context">application context</param>
        /// <typeparam name="T">type you wish to convert result to</typeparam>
        public static T Single<T> (Node node, ApplicationContext context)
        {
            return Single<T> (node, node, context);
        }
        
        /// <summary>
        /// returns a single value from the given node. if node has an expression as its value, it
        /// will iterate the results of that expression, concatenating the results back to one value, before
        /// converting to T. if node contains anything but an expression, it will return that object as type T
        /// back to caller. data source node is used as source for any formatting expressions inside of node
        /// </summary>
        /// <param name="node">node</param>
        /// <param name="dataSource">node to use as source for any formatting expressions inside of node</param>
        /// <param name="context">application context</param>
        /// <typeparam name="T">type you wish to convert result to</typeparam>
        public static T Single<T> (Node node, Node dataSource, ApplicationContext context)
        {
            if (IsExpression (node.Value)) {
                string exp = IsFormatted (node) ? FormatNode (node, dataSource, context) : node.Get<string> (context);
                return Single<T> (exp, dataSource, context);
            } else {
                object value = IsFormatted (node) ? FormatNode (node, dataSource, context) : node.Value;
                return Utilities.Convert<T> (value, context);
            }
        }

        /// <summary>
        /// returns a single value of type T according to the given expression. if expression yields
        /// multiple results, the values from each result will be concatenated as string before attempting to
        /// convert the value to type T
        /// </summary>
        /// <param name="expression">expression</param>
        /// <param name="dataSource">root node for expression</param>
        /// <param name="context">application context</param>
        /// <typeparam name="T">type to return result of expression as</typeparam>
        public static T Single<T> (string expression, Node dataSource, ApplicationContext context)
        {
            if (!IsExpression (expression))
                throw new ArgumentException ("Single was not given a valid expression");

            // evaluating expression
            var match = Expression.Create (expression).Evaluate (dataSource, context);

            // returning simple count, if expression is of type count
            if (match.TypeOfMatch == Match.MatchType.count)
                return Utilities.Convert<T> (match.Count, context);

            // checking if this is a single object match, at which case we don't iterate converting to string
            if (match.Count == 1)
                return Utilities.Convert<T> (match [0].Value, context);

            // looping through each match result, concatenating all values,
            // before converting to requested type, and returning result back to caller
            string retVal = null;
            foreach (var idxMatch in match) {
                retVal += Utilities.Convert<string> (idxMatch.Value, context);
            }
            return Utilities.Convert<T> (retVal, context);
        }

        /// <summary>
        /// will iterate node's value expression result, if node's value is an expression,
        /// otherwise it will invoke your callback once for the value of node, converted
        /// to type T
        /// </summary>
        /// <param name="node">node to use</param>
        /// <param name="context">application context</param>
        /// <param name="functor">delegate to invoke for the converted result</param>
        /// <typeparam name="T">type to convert results to</typeparam>
        public static void Iterate<T> (
            Node node,
            ApplicationContext context,
            IteratorCallback<T> functor)
        {
            Iterate<T> (node, node, context, functor);
        }
        
        /// <summary>
        /// will iterate node's value expression result, if node's value is an expression,
        /// otherwise it will invoke your callback once for the value of node, converted
        /// to type T
        /// </summary>
        /// <param name="node">node to use as source of expression</param>
        /// <param name="node">node to use as data source for formatting values and where to execute the expression</param>
        /// <param name="context">application context</param>
        /// <param name="functor">delegate to invoke for the converted result</param>
        /// <typeparam name="T">type to convert results to</typeparam>
        public static void Iterate<T> (
            Node node,
            Node dataSource,
            ApplicationContext context,
            IteratorCallback<T> functor)
        {
            if (IsExpression (node.Value)) {
                string exp = IsFormatted (node) ? FormatNode (node, dataSource, context) : node.Get<string> (context);
                Iterate<T> (exp, dataSource, context, functor);
            } else {
                object value = IsFormatted (node) ? FormatNode (node, dataSource, context) : node.Value;
                functor (Utilities.Convert<T> (value, context));
            }
        }

        /// <summary>
        /// iterates the given expression with the given datasource as root node for expression and
        /// invokes your delegate once for each result in the expression, converting the expression's
        /// value to type T
        /// </summary>
        /// <param name="expression">expression</param>
        /// <param name="dataSource">root node for expression</param>
        /// <param name="context">application context</param>
        /// <param name="functor">delegate to invoke for your expression result</param>
        /// <typeparam name="T">type to convert expression's result to</typeparam>
        public static void Iterate<T> (
            string expression, 
            Node dataSource, 
            ApplicationContext context, 
            IteratorCallback<T> functor)
        {
            if (!IsExpression (expression))
                throw new ArgumentException ("Iterate was not given a valid expression");

            var match = Expression.Create (expression).Evaluate (dataSource, context);
            foreach (var idxMatch in match) {
                functor (Utilities.Convert<T> (idxMatch.Value, context));
            }
        }

        /// <summary>
        /// iterates through expression in node's value, with given node as datasource, and invokes functor
        /// for every single match, passing in a MatchEntity, wrapping the match item
        /// </summary>
        /// <param name="node">node to use for expression and datasource</param>
        /// <param name="context">application context</param>
        /// <param name="functor">delegate invoked once for every single match</param>
        public static void Iterate (
            Node node,
            ApplicationContext context,
            IteratorCallback<MatchEntity> functor)
        {
            Iterate (node, node, context, functor);
        }
        
        /// <summary>
        /// iterates through expression in node's value, with dataSource as data source, and invokes functor
        /// for every single match, passing in a MatchEntity, wrapping the match item
        /// </summary>
        /// <param name="node">node to use for expression</param>
        /// <param name="dataSource">node to use as datasource</param>
        /// <param name="context">application context</param>
        /// <param name="functor">delegate invoked once for every single match</param>
        public static void Iterate (
            Node node,
            Node dataSource,
            ApplicationContext context,
            IteratorCallback<MatchEntity> functor)
        {
            string exp = IsFormatted (node) ? FormatNode (node, dataSource, context) : node.Get<string> (context);
            Iterate (exp, dataSource, context, functor);
        }

        /// <summary>
        /// iterates through given expression, with given node as datasource, and invokes functor
        /// for every single match, passing in a MatchEntity, wrapping the match item
        /// </summary>
        /// <param name="expression">expression</param>
        /// <param name="dataSource">data source node to use for expression</param>
        /// <param name="context">application context</param>
        /// <param name="functor">delegate invoked once for every single match</param>
        public static void Iterate (
            string expression,
            Node dataSource,
            ApplicationContext context,
            IteratorCallback<MatchEntity> functor)
        {
            if (!IsExpression (expression))
                throw new ArgumentException ("Iterate was not given a valid expression");

            var match = Expression.Create (expression).Evaluate (dataSource, context);
            foreach (var idxMatch in match) {
                functor (idxMatch);
            }
        }

        /// <summary>
        /// returns content of node as list of T
        /// </summary>
        /// <param name="node">node either containing an expression or a list of children nodes</param>
        /// <param name="context">application context</param>
        /// <typeparam name="T">type of object you wish to retrieve</typeparam>
        public static IEnumerable<T> Content<T> (Node node, ApplicationContext context)
        {
            return Content<T> (node, node, context);
        }

        /// <summary>
        /// returns content of node as list of T
        /// </summary>
        /// <param name="node">node either containing an expression or a list of children nodes</param>
        /// <param name="dataSource">node being dataSource</param>
        /// <param name="context">application context</param>
        /// <typeparam name="T">type of object you wish to retrieve</typeparam>
        public static IEnumerable<T> Content<T> (Node node, Node dataSource, ApplicationContext context)
        {
            if (IsExpression (node.Value)) {
                string exp = IsFormatted (node) ? FormatNode (node, dataSource, context) : node.Get<string> (context);
                foreach (var idx in Content<T> (exp, dataSource, context)) {
                    yield return idx;
                }
            } else if (typeof(T) == typeof(Node)) {
                foreach (Node idx in node.Children) {
                    yield return Utilities.Convert<T> (idx, context);
                }
            } else {
                foreach (Node idx in node.Children) {
                    yield return idx.Get<T> (context);
                }
            }
        }

        public static IEnumerable<T> Content<T> (string expression, Node dataSource, ApplicationContext context)
        {
            if (!IsExpression (expression))
                throw new ArgumentException ("Content was not given a valid expression");

            var match = Expression.Create (expression).Evaluate (dataSource, context);
            foreach (var idx in match) {
                yield return Utilities.Convert<T> (idx.Value, context);
            }
        }

        /// <summary>
        /// returns all matches from expression in node
        /// </summary>
        /// <param name="node">node being both expression node and data source node</param>
        /// <param name="context">application context</param>
        public static IEnumerable<MatchEntity> Matches (Node node, ApplicationContext context)
        {
            return Matches (node, node, context);
        }

        /// <summary>
        /// returns all matches from expression in node
        /// </summary>
        /// <param name="node">node being expression node</param>
        /// <param name="dataSource">node being data source node</param>
        /// <param name="context">application context</param>
        public static IEnumerable<MatchEntity> Matches (Node node, Node dataSource, ApplicationContext context)
        {
            string exp = IsFormatted (node) ? FormatNode (node, dataSource, context) : node.Get<string> (context);
            return Matches (exp, dataSource, context);
        }

        /// <summary>
        /// returns all matches from given expression
        /// </summary>
        /// <param name="expression">expression</param>
        /// <param name="dataSource">node being data source node</param>
        /// <param name="context">application context</param>
        public static IEnumerable<MatchEntity> Matches (string expression, Node dataSource, ApplicationContext context)
        {
            if (!IsExpression (expression))
                throw new ArgumentException ("Matches was not given a valid expression");

            var match = Expression.Create (expression).Evaluate (dataSource, context);
            foreach (var idx in match) {
                yield return idx;
            }
        }

        /*
         * helper method to recursively format node's value
         */
        private static string FormatNodeRecursively (Node node, Node dataSource, ApplicationContext context)
        {
            bool isFormatted = IsFormatted (node);
            bool isExpression = IsExpression (node.Value);

            if (isExpression && isFormatted) {

                // node is recursively formatted, and also an expression. formating node first, then evaluating expression
                return Single<string> (FormatNode (node, dataSource, context), dataSource, context) ?? ""; // cannot return null here, in case expression yields null
            } else if (isFormatted) {

                // node is formatted recursively, but not an expression
                return FormatNode (node, dataSource, context);
            } else if (isExpression) {

                // node is an expression, but not formatted
                return Single<string> (node.Get<string> (context), dataSource, context);
            } else {

                // node is neither an expression, returning node's value, defaulting to string.Empty if no value exists
                return node.Get<string> (context, string.Empty);
            }
        }
    }
}