﻿/*----------------------------------------------------------------
 *  Copyright (c) ThoughtWorks, Inc.
 *  Licensed under the Apache License, Version 2.0
 *  See LICENSE.txt in the project root for license information.
 *----------------------------------------------------------------*/


using Gauge.Dotnet.Models;
using Gauge.Dotnet.Processors;
using Gauge.Messages;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Gauge.Dotnet.UnitTests.Processors
{
    [TestFixture]
    public class ExecuteStepProcessorTests
    {
        public void Foo(string param)
        {
        }

        [Test]
        public void ShouldProcessExecuteStepRequest()
        {
            const string parsedStepText = "Foo";
            var request = new ExecuteStepRequest
            {
                ActualStepText = parsedStepText,
                ParsedStepText = parsedStepText,
                Parameters =
                    {
                        new Parameter
                        {
                            ParameterType = Parameter.Types.ParameterType.Static,
                            Name = "Foo",
                            Value = "Bar"
                        }
                    }
            };
            var mockStepRegistry = new Mock<IStepRegistry>();
            mockStepRegistry.Setup(x => x.ContainsStep(parsedStepText)).Returns(true);
            var fooMethodInfo = new GaugeMethod { Name = "Foo", ParameterCount = 1 };
            mockStepRegistry.Setup(x => x.MethodFor(parsedStepText)).Returns(fooMethodInfo);
            var mockOrchestrator = new Mock<IExecutionOrchestrator>();
            mockOrchestrator.Setup(e => e.ExecuteStep(fooMethodInfo, It.IsAny<string[]>()))
                .Returns(() => new ProtoExecutionResult { ExecutionTime = 1, Failed = false });

            var mockTableFormatter = new Mock<ITableFormatter>();

            var processor = new ExecuteStepProcessor(mockStepRegistry.Object, mockOrchestrator.Object, mockTableFormatter.Object);
            var response = processor.Process(request);

            ClassicAssert.False(response.ExecutionResult.Failed);
        }

        [Test]
        [TestCase(Parameter.Types.ParameterType.Table)]
        [TestCase(Parameter.Types.ParameterType.SpecialTable)]
        public void ShouldProcessExecuteStepRequestForTableParam(Parameter.Types.ParameterType parameterType)
        {
            const string parsedStepText = "Foo";
            var protoTable = new ProtoTable();
            var tableJSON = "{'headers':['foo', 'bar'],'rows':[['foorow1','barrow1']]}";
            var request = new ExecuteStepRequest
            {
                ActualStepText = parsedStepText,
                ParsedStepText = parsedStepText,
                Parameters =
                    {
                        new Parameter
                        {
                            ParameterType = parameterType,
                            Table = protoTable
                        }
                    }
            };

            var mockStepRegistry = new Mock<IStepRegistry>();
            mockStepRegistry.Setup(x => x.ContainsStep(parsedStepText)).Returns(true);
            var fooMethodInfo = new GaugeMethod { Name = "Foo", ParameterCount = 1 };
            mockStepRegistry.Setup(x => x.MethodFor(parsedStepText)).Returns(fooMethodInfo);
            var mockOrchestrator = new Mock<IExecutionOrchestrator>();
            mockOrchestrator.Setup(e => e.ExecuteStep(fooMethodInfo, It.IsAny<string[]>())).Returns(() =>
                new ProtoExecutionResult
                {
                    ExecutionTime = 1,
                    Failed = false
                });

            var mockAssemblyLoader = new Mock<IAssemblyLoader>();
            mockAssemblyLoader.Setup(x => x.GetLibType(LibType.MessageCollector));
            var mockTableFormatter = new Mock<ITableFormatter>();
            mockTableFormatter.Setup(x => x.GetJSON(protoTable))
                .Returns(tableJSON);
            var processor = new ExecuteStepProcessor(mockStepRegistry.Object, mockOrchestrator.Object, mockTableFormatter.Object);
            var response = processor.Process(request);

            mockOrchestrator.Verify(executor =>
                executor.ExecuteStep(fooMethodInfo, It.Is<string[]>(strings => strings[0] == tableJSON)));
            ClassicAssert.False(response.ExecutionResult.Failed);
        }

        [Test]
        public void ShouldReportArgumentMismatch()
        {
            const string parsedStepText = "Foo";
            var request = new ExecuteStepRequest
            {
                ActualStepText = parsedStepText,
                ParsedStepText = parsedStepText
            };
            var mockStepRegistry = new Mock<IStepRegistry>();
            mockStepRegistry.Setup(x => x.ContainsStep(parsedStepText)).Returns(true);
            var fooMethod = new GaugeMethod { Name = "Foo", ParameterCount = 1 };
            mockStepRegistry.Setup(x => x.MethodFor(parsedStepText)).Returns(fooMethod);
            var mockOrchestrator = new Mock<IExecutionOrchestrator>();

            var mockTableFormatter = new Mock<ITableFormatter>();

            var processor = new ExecuteStepProcessor(mockStepRegistry.Object, mockOrchestrator.Object, mockTableFormatter.Object);
            var response = processor.Process(request);

            ClassicAssert.True(response.ExecutionResult.Failed);
            ClassicAssert.AreEqual(response.ExecutionResult.ErrorMessage,
                "Argument length mismatch for Foo. Actual Count: 0, Expected Count: 1");
        }

        [Test]
        public void ShouldReportMissingStep()
        {
            const string parsedStepText = "Foo";
            var request = new ExecuteStepRequest
            {
                ActualStepText = parsedStepText,
                ParsedStepText = parsedStepText
            };
            var mockStepRegistry = new Mock<IStepRegistry>();
            mockStepRegistry.Setup(x => x.ContainsStep(parsedStepText)).Returns(false);
            var mockOrchestrator = new Mock<IExecutionOrchestrator>();
            var mockTableFormatter = new Mock<ITableFormatter>();

            var processor = new ExecuteStepProcessor(mockStepRegistry.Object, mockOrchestrator.Object, mockTableFormatter.Object);
            var response = processor.Process(request);

            ClassicAssert.True(response.ExecutionResult.Failed);
            ClassicAssert.AreEqual(response.ExecutionResult.ErrorMessage,
                "Step Implementation not found");
        }
    }
}