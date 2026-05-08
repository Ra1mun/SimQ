using Xunit;
using SimQCore;

namespace SimQ.Tests
{
    public class ErrorMessageTests
    {
        [Fact]
        public void ErrorMsg_InitiallyEmpty()
        {
            var em = new ErrorMessage();
            Assert.Equal(string.Empty, em.ErrorMsg);
        }

        [Fact]
        public void Add_ErrorMsg_AppendsMessage()
        {
            var em = new ErrorMessage();
            em.Add_ErrorMsg("Error1");

            Assert.Contains("Error1", em.ErrorMsg);
        }

        [Fact]
        public void Add_ErrorMsg_MultipleCalls_AccumulatesMessages()
        {
            var em = new ErrorMessage();
            em.Add_ErrorMsg("Error1");
            em.Add_ErrorMsg("Error2");

            Assert.Contains("Error1", em.ErrorMsg);
            Assert.Contains("Error2", em.ErrorMsg);
        }

        [Fact]
        public void Add_ErrorMsg_FromNewLine_AddsNewLine()
        {
            var em = new ErrorMessage();
            em.Add_ErrorMsg("Error1", fromNewLine: true);

            Assert.StartsWith("\n", em.ErrorMsg);
        }
    }
}
