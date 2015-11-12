/*
 * Phosphorus Five, copyright 2014 - 2015, Thomas Hansen, phosphorusfive@gmail.com
 * Phosphorus Five is licensed under the terms of the MIT license, see the enclosed LICENSE file for details
 */

using NUnit.Framework;
using p5.core;

namespace p5.unittests.lambda
{
    /// <summary>
    ///     unit tests for testing the [eval] keyword
    /// </summary>
    [TestFixture]
    public class Eval : TestBase
    {
        public Eval ()
            : base ("p5.lambda", "p5.types", "p5.hyperlisp") { }

        /// <summary>
        ///     verifies that [eval] is immutable
        /// </summary>
        [Test]
        public void EvalIsMutable ()
        {
            var result = ExecuteLambda (@"_data:success
set:x:/-?value
  src:error");
            Assert.AreEqual (0, result.Count);
        }
        
        /// <summary>
        ///     verifies that [eval] can return nodes
        /// </summary>
        [Test]
        public void EvalCanReturnNodes ()
        {
            var result = ExecuteLambda (@"add:x:/..
  src
    _foo1:bar1
    _foo2:bar2");
            Assert.AreEqual (2, result.Count);
            Assert.AreEqual ("_foo1", result [0].Name);
            Assert.AreEqual ("bar1", result [0].Value);
            Assert.AreEqual ("_foo2", result [1].Name);
            Assert.AreEqual ("bar2", result [1].Value);
        }
        
        /// <summary>
        ///     verifies that [eval] can return value
        /// </summary>
        [Test]
        public void EvalCanReturnValue ()
        {
            var result = ExecuteLambda (@"set:x:/..?value
  src:success");
            Assert.AreEqual (0, result.Count);
            Assert.AreEqual ("success", result.Value);
        }

        /// <summary>
        ///     verifies that [eval] can execute expression, and return nodes back to caller
        /// </summary>
        [Test]
        public void EvalCanExecuteExpressionAndReturnNodes ()
        {
            var result = ExecuteLambda (@"_x
  add:x:/.
    src
      _foo1:bar1
eval:x:/-
add:x:/..
  src:x:/./-/*");
            Assert.AreEqual (1, result.Count);
            Assert.AreEqual ("_foo1", result [0].Name);
            Assert.AreEqual ("bar1", result [0].Value);
        }
        
        /// <summary>
        ///     verifies that [eval] can execute expression, and return value back to caller
        /// </summary>
        [Test]
        public void EvalCanExecuteExpressionAndReturnValue ()
        {
            var result = ExecuteLambda (@"_x
  set:x:/.?value
    src:""_foo:bar""
eval:x:/-
add:x:/..
  src:x:/./-?value");
            Assert.AreEqual (1, result.Count);
            Assert.AreEqual ("_foo", result [0].Name);
            Assert.AreEqual ("bar", result [0].Value);
        }

        [ActiveEvent (Name = "eval.test1")]
        private static void eval_test1 (ApplicationContext context, ActiveEventArgs e)
        {
            e.Args.Value = "foo2";
        }
        
        /// <summary>
        ///     verifies that [eval] will execute nodes returned
        /// </summary>
        [Test]
        public void EvalWillExecuteAddedReturnNodes ()
        {
            var result = ExecuteLambda (@"_x
  add:x:/.
    src
      eval.test1:foo1
eval:x:/-
add:x:/..
  src:x:/./-/*");
            Assert.AreEqual (1, result.Count);
            Assert.AreEqual ("eval.test1", result [0].Name);
            Assert.AreEqual ("foo2", result [0].Value);
        }
        
        /// <summary>
        ///     verifies that [eval] will not execute inserted nodes returned
        /// </summary>
        [Test]
        public void EvalWillNotExecuteInsertedReturnNodes ()
        {
            var result = ExecuteLambda (@"_x
  insert-before:x:
    src
      eval.test1:foo1
eval:x:/-
insert-before:x:/../0
  src:x:/./-/*");
            Assert.AreEqual (1, result.Count);
            Assert.AreEqual ("eval.test1", result [0].Name);
            Assert.AreEqual ("foo1", result [0].Value);
        }
        
        /// <summary>
        ///     verifies that [eval] can be source of [add]
        /// </summary>
        [Test]
        public void EvalCanBeSourceOfAdd ()
        {
            var result = ExecuteLambda (@"_x
  insert-before:x:
    src:_foo1
add:x:/..
  eval:x:/./-");
            Assert.AreEqual (1, result.Count);
            Assert.AreEqual ("_foo1", result [0].Name);
            Assert.IsNull (result [0].Value);
        }
        
        /// <summary>
        ///     verifies that [eval] can be source of [add]
        /// </summary>
        [Test]
        public void EvalCanTakeInputArgs ()
        {
            var result = ExecuteLambda (@"_x
  set:x:/+/0?value
    src:x:/../*/_input?value
  add:x:/..
    src
eval:x:/-
  _input:_foo1
add:x:/..
  src:x:/./-/*");
            Assert.AreEqual (1, result.Count);
            Assert.AreEqual ("_foo1", result [0].Name);
            Assert.IsNull (result [0].Value);
        }
    }
}