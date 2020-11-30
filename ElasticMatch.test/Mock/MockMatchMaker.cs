namespace ElasticMatch.test.Mock
{
    public class MockMatchMaker : IMatchMaker
    {
        private string _helloMsg;

        public void Hello(string msg)
        {
            _helloMsg = msg;
        }

        public string GetHello()
        {
            return _helloMsg;
        }
    }
}