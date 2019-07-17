public class NNetErrorImpl : IHandleNetError
{
    public bool HandleNetError(int code, string name, ENetType type, byte[] results)
    {
        bool ret = true;
        if (code != 0)
        {

        }
        else
        {
            /// code是0表示没有错误
            ret = false;
        }
        return ret;
    }
}
