using Microsoft.Extensions.Options;
using Moq;
using RiskFirst.Hateoas.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace RiskFirst.Hateoas.Tests
{
    [Trait("Category","Evaluation")]
    public class DefaultLinksEvaluatorTests
    {
        private LinksEvaluatorTestCase ConfigureTestCase(Action<TestCaseBuilder> configureTest)
        {
            var builder = new TestCaseBuilder();
            configureTest?.Invoke(builder);
            return builder.BuildLinksEvaluatorTestCase();
        }

        [AutonamedFact]
        public void GivenMockTransformations_TransformIsExecuted()
        {
            // Arrange
            var testCase = ConfigureTestCase(builder =>
            {
                builder.UseMockHrefTransformation(config =>
                {
                    config.Setup(x => x.Transform(It.IsAny<LinkTransformationContext>())).Returns("href");
                })
                .UseMockRelTransformation(config =>
                {
                    config.Setup(x => x.Transform(It.IsAny<LinkTransformationContext>())).Returns("rel");
                });
            });
            var mockLinkSpec = new Mock<ILinkSpec>();
            mockLinkSpec.SetupGet(x => x.Id).Returns("testLink");
            mockLinkSpec.SetupGet(x => x.HttpMethod).Returns(HttpMethod.Get);

            // Act
            var model = new TestLinkContainer();
            testCase.UnderTest.BuildLinks(new[] { mockLinkSpec.Object }, model);

            // Assert
            Assert.True(model.Links.Count == 1, "Incorrect number of links applied");
            Assert.Equal("href", model.Links["testLink"].Href);
            Assert.Equal("rel", model.Links["testLink"].Rel);

            testCase.HrefTransformMock.Verify(x => x.Transform(It.IsAny<LinkTransformationContext>()), Times.Once());
            testCase.RelTransformMock.Verify(x => x.Transform(It.IsAny<LinkTransformationContext>()), Times.Once());

        }

        [AutonamedFact]
        public void GivenLinkBuilderTransform_HrefIsBuilt()
        {
            // Arrange
            var testCase = ConfigureTestCase(builder =>
            {
                builder.UseLinkBuilderHrefTransformation(href => href.Add("a").Add(ctx => "b").Add("c"))
                        .UseMockRelTransformation(null);
            });
            var mockLinkSpec = new Mock<ILinkSpec>();
            mockLinkSpec.SetupGet(x => x.Id).Returns("testLink");
            mockLinkSpec.SetupGet(x => x.HttpMethod).Returns(HttpMethod.Get);

            // Act
            var model = new TestLinkContainer();
            testCase.UnderTest.BuildLinks(new[] { mockLinkSpec.Object }, model);

            // Assert
            Assert.True(model.Links.Count == 1, "Incorrect number of links applied");
            Assert.Equal("abc", model.Links["testLink"].Href);

            testCase.RelTransformMock.Verify(x => x.Transform(It.IsAny<LinkTransformationContext>()), Times.Once());
        }

        [AutonamedFact]
        public void GivenLinkBuilderTransform_UsingDefaultAddProtocol_HrefIsBuiltUsingRequestScheme()
        {
            // Arrange
            var testCase = ConfigureTestCase(builder =>
            {
                builder.WithRequestScheme(Uri.UriSchemeHttp);
                builder.UseLinkBuilderHrefTransformation(href => href.AddProtocol())
                        .UseMockRelTransformation(null);
            });
            var mockLinkSpec = new Mock<ILinkSpec>();
            mockLinkSpec.SetupGet(x => x.Id).Returns("testLink");
            mockLinkSpec.SetupGet(x => x.HttpMethod).Returns(HttpMethod.Get);

            // Act
            var model = new TestLinkContainer();
            testCase.UnderTest.BuildLinks(new[] { mockLinkSpec.Object }, model);

            // Assert
            Assert.Equal("http://", model.Links["testLink"].Href);

            testCase.RelTransformMock.Verify(x => x.Transform(It.IsAny<LinkTransformationContext>()), Times.Once());
        }

        [AutonamedFact]
        public void GivenLinkBuilderTransform_UsingOverriddenAddProtocol_HrefIsBuiltUsingProvidedScheme()
        {
            // Arrange
            var testCase = ConfigureTestCase(builder =>
            {
                builder.WithRequestScheme(Uri.UriSchemeHttp);
                builder.UseLinkBuilderHrefTransformation(href => href.AddProtocol(Uri.UriSchemeHttps))
                        .UseMockRelTransformation(null);
            });
            var mockLinkSpec = new Mock<ILinkSpec>();
            mockLinkSpec.SetupGet(x => x.Id).Returns("testLink");
            mockLinkSpec.SetupGet(x => x.HttpMethod).Returns(HttpMethod.Get);

            // Act
            var model = new TestLinkContainer();
            testCase.UnderTest.BuildLinks(new[] { mockLinkSpec.Object }, model);

            // Assert
            Assert.Equal("https://", model.Links["testLink"].Href);

            testCase.RelTransformMock.Verify(x => x.Transform(It.IsAny<LinkTransformationContext>()), Times.Once());
        }

        [AutonamedFact]
        public void GivenExceptionThrowingHrefTransformation_ThowsLinkTransformationException()
        {
            // Arrange
            var testCase = ConfigureTestCase(builder =>
            {
                builder.UseMockHrefTransformation(mock =>
                {
                    mock.Setup(x => x.Transform(It.IsAny<LinkTransformationContext>())).Throws<InvalidOperationException>();
                })
                .UseMockRelTransformation(mock =>
                {
                    mock.Setup(x => x.Transform(It.IsAny<LinkTransformationContext>())).Returns("rel");
                });
            });
            var mockLinkSpec = new Mock<ILinkSpec>();
            mockLinkSpec.SetupGet(x => x.Id).Returns("testLink");
            mockLinkSpec.SetupGet(x => x.HttpMethod).Returns(HttpMethod.Get);

            // Act
            var model = new TestLinkContainer();
            Assert.Throws<LinkTransformationException>(() =>
            {
                testCase.UnderTest.BuildLinks(new[] { mockLinkSpec.Object }, model);
            });
        }

        [AutonamedFact]
        public void GivenExceptionThrowingRelTransformation_ThowsLinkTransformationException()
        {
            // Arrange
            var testCase = ConfigureTestCase(builder =>
            {
                builder.UseMockHrefTransformation(mock =>
                {
                    mock.Setup(x => x.Transform(It.IsAny<LinkTransformationContext>())).Returns("href");
                })
                .UseLinkBuilderRelTransformation(config =>
                {
                    config.Add(ctx => throw new InvalidOperationException());
                });
            });
            var mockLinkSpec = new Mock<ILinkSpec>();
            mockLinkSpec.SetupGet(x => x.Id).Returns("testLink");
            mockLinkSpec.SetupGet(x => x.HttpMethod).Returns(HttpMethod.Get);

            // Act
            var model = new TestLinkContainer();
            Assert.Throws<LinkTransformationException>(() =>
            {
                testCase.UnderTest.BuildLinks(new[] { mockLinkSpec.Object }, model);
            });
        }
    }
}
