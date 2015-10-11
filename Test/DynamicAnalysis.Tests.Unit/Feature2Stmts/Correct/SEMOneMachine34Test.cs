﻿//-----------------------------------------------------------------------
// <copyright file="SEMOneMachine34Test.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.DynamicAnalysis.Tests.Unit
{
    [TestClass]
    public class SEMOneMachine34Test : BasePSharpTest
    {
        [TestMethod]
        public void TestSEMOneMachine34()
        {
            var test = @"
using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace SystematicTesting
{
    class E1 : Event {
        public Tuple<int, bool> T;
        public E1(Tuple<int, bool> t) : base(-1, -1) { this.T = t; }
    }

    class E2 : Event {
        public int V;
        public bool B;
        public E2(int v, bool b) : base(-1, -1) { this.V = v; this.B = b; }
    }

    class E3 : Event {
        public int V;
        public E3(int v) : base(-1, -1) { this.V = v; }
    }

    class E4 : Event {
        public Dictionary<int, int> D;
        public List<bool> L;
        public E4(Dictionary<int, int> d, List<bool> l) : base(-1, -1) { this.D = d; this.L = l; }
    }

    class MachOS : Machine
    {
        int Int;
        bool Bool;
        MachineId mach;
        Dictionary<int, int> m;
        List<bool> s;

        [Start]
        [OnEntry(nameof(EntryInit))]
        [OnEventDoAction(typeof(E1), nameof(Foo1))]
        [OnEventDoAction(typeof(E2), nameof(Foo2))]
        [OnEventDoAction(typeof(E3), nameof(Foo3))]
        [OnEventDoAction(typeof(E4), nameof(Foo4))]
        class Init : MachineState { }

        void EntryInit()
        {
            m = new Dictionary<int, int>();
            s = new List<bool>();
            m.Add(0, 1);
            m.Add(1, 2);
			s.Add(true);
			s.Add(false);
			s.Add(true);
			this.Send(this.Id, new E1(Tuple.Create(1, true)));
			this.Send(this.Id, new E2(0, false));
            this.Send(this.Id, new E3(1));
			this.Send(this.Id, new E4(m, s));
        }

        void Foo1()
        {
            Int = (this.ReceivedEvent as E1).T.Item1;
            this.Assert(Int == 1);
            Bool = (this.ReceivedEvent as E1).T.Item2;
            this.Assert(Bool == true);
        }

        void Foo2()
        {
            Int = (this.ReceivedEvent as E2).V;
            this.Assert(Int == 0);
            Bool = (this.ReceivedEvent as E2).B;
            this.Assert(Bool == false);
        }

        void Foo3()
        {
            Int = (this.ReceivedEvent as E3).V;
            this.Assert(Int == 1);
        }

        void Foo4()
        {
            Int = (this.ReceivedEvent as E4).D[0];
            this.Assert(Int == 1);
            Bool = (this.ReceivedEvent as E4).L[2];
            this.Assert(Bool == true);
        }
    }

    public static class TestProgram
    {
        public static void Main(string[] args)
        {
            TestProgram.Execute();
            Console.ReadLine();
        }

        [Test]
        public static void Execute()
        {
            PSharpRuntime.CreateMachine(typeof(MachOS));
        }
    }
}";

            var parser = new CSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), true);
            var program = parser.Parse();
            program.Rewrite();

            var sctConfig = DynamicAnalysisConfiguration.Create();
            sctConfig.SuppressTrace = true;
            sctConfig.Verbose = 2;
            sctConfig.SchedulingStrategy = SchedulingStrategy.DFS;
            sctConfig.SchedulingIterations = 100;

            var assembly = base.GetAssembly(program.GetSyntaxTree());
            var context = AnalysisContext.Create(sctConfig, assembly);
            var sctEngine = SCTEngine.Create(context).Run();

            Assert.AreEqual(0, sctEngine.NumOfFoundBugs);
        }
    }
}
