using AutoFixture;
using AutoFixture.AutoMoq;
using cddo_users.Logic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Notify.Client;
using Notify.Exceptions;
using Notify.Interfaces;
using Notify.Models.Responses;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace cddo_users.test.Services
{
    [TestFixture]
    public class EmailServiceTests
    {
        private readonly Mock<IConfiguration> configurationMock;
        private readonly Mock<INotificationClient> _notificationClientMock;
        protected readonly IFixture fixture;

        public EmailServiceTests()
        {
            configurationMock = new Mock<IConfiguration>();
            _notificationClientMock = new Mock<INotificationClient>();
            fixture = new Fixture().Customize(new AutoMoqCustomization());
        }

        [Test]
        public void SendEmail_WithTemplateId_EmailSent()
        {
            
            //Arrange
            var sut = new EmailService(configurationMock.Object, _notificationClientMock.Object);
            var methods = sut.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach ( var method in methods )
            {
                //Arrange
                var response = fixture.Create<EmailNotificationResponse>();
                var templateId = fixture.Create<string>();
                _notificationClientMock.Invocations.Clear();

                var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                //Act
                _notificationClientMock.Setup(n => n.SendEmail(It.IsAny<string>(),
                                                            It.IsAny<string>(),
                                                            It.IsAny<Dictionary<string, dynamic>>(),
                                                            It.IsAny<string>(),
                                                            It.IsAny<string>())).Returns(response);

                var parameters = method.GetParameters();

                object[]? parameterValues = parameters.Select( p => GenerateValue(p.ParameterType)!).ToArray();

                method.Invoke(sut, parameterValues);

                //Assert
                var consoleOutput = stringWriter.ToString().Trim();
                _notificationClientMock.Verify(n => n.SendEmail(It.IsAny<string>(),
                                                             It.IsAny<string>(),
                                                             It.IsAny<Dictionary<string, dynamic>>(),
                                                             It.IsAny<string>(),
                                                             It.IsAny<string>()), Times.Once);
                consoleOutput.Should().Be($"Email sent. Notification ID: {response.id}");

            }

            //sut.SendEmail(fixture.Create<string>(), fixture.Create<Dictionary<string, dynamic>>(), templateId);

            
        }

        [Test]
        public void SendWelcomeEmail_WhenExceptionIsThrown_WriteErrorInConsole()
        {
            //Arrange
            var sut = new EmailService(configurationMock.Object, _notificationClientMock.Object);
            var methods = sut.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var exception = new NotifyClientException("Oii cant park there mate!");

            foreach (var method in methods)
            {
                //Arrange
                var response = fixture.Create<EmailNotificationResponse>();
                var templateId = fixture.Create<string>();
                _notificationClientMock.Invocations.Clear();

                var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                _notificationClientMock.Setup(n => n.SendEmail(It.IsAny<string>(),
                                                         It.IsAny<string>(),
                                                         It.IsAny<Dictionary<string, dynamic>>(),
                                                         It.IsAny<string>(),
                                                         It.IsAny<string>())).Throws(exception);

                var parameters = method.GetParameters();

                object[]? parameterValues = parameters.Select(p => GenerateValue(p.ParameterType)!).ToArray();

                method.Invoke(sut, parameterValues);

                //Assert
                var consoleOutput = stringWriter.ToString().Trim();
                _notificationClientMock.Verify(n => n.SendEmail(It.IsAny<string>(),
                                                             It.IsAny<string>(),
                                                             It.IsAny<Dictionary<string, dynamic>>(),
                                                             It.IsAny<string>(),
                                                             It.IsAny<string>()), Times.Once);

                consoleOutput.Should().Be($"An error occurred: {exception.Message}");
            }
        }

        private object? GenerateValue(Type type)
        {
            if(type == typeof(string)) return fixture.Create<string>();
            if(type == typeof(Dictionary<string, dynamic>)) return fixture.Create<Dictionary<string, dynamic>>();

            return null;
        }
    }
}
