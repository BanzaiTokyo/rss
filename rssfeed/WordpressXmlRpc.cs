using System;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace WP7toWordpressXMLRPC
{
    using System.Reflection;
    using CookComputing.XmlRpc;

    /*class MethodBase
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public MethodBase GetCurrentMethod()
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);

            return sf.GetMethod();
        }
    }*/

    [XmlRpcUrl(WordpressWrapper.BlogUrl)]
    public class WordpressWrapper : XmlRpcClientProtocol
    {
        public const string BlogUrl = "";//http://3dprint.tw/xmlrpc.php";

        public MethodInfo GetMethodInfo(string MethodName, object[] parameters) {
            Type type = this.GetType();
            Type[] paramTypes = new Type[0];
            if (parameters != null)
            {
                paramTypes = new Type[parameters.Length + 1];
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i] == null)
                        throw new XmlRpcNullParameterException("Null parameters are invalid");
                    paramTypes[i] = parameters[i].GetType();
                }
                AsyncCallback acb = (IAsyncResult ar) => { };
                paramTypes[paramTypes.Length - 1] = acb.GetType();
            }
            return type.GetRuntimeMethod(MethodName, paramTypes);
        }

        [XmlRpcBegin("metaWeblog.getRecentPosts")]
        public IAsyncResult BeginGetRecentPosts(int blogid, string username, string password, int numposts, AsyncCallback acb)
        {
            var parameters = new object[] { 0, username, password, numposts};
            var mi = this.GetMethodInfo("BeginGetRecentPosts", parameters);
            return this.BeginInvoke(mi, parameters, this, acb, null);
        }

        [XmlRpcEnd]
        public XmlRpcStruct[] EndGetRecentPosts(IAsyncResult iasr)
        {
            XmlRpcStruct[] ret = (XmlRpcStruct[])this.EndInvoke(iasr);
            return ret;
        }

        [XmlRpcBegin("metaWeblog.newPost")]
        public IAsyncResult BeginNewPost(int blogid, string username, string password, XmlRpcStruct post, bool publish, AsyncCallback acb)
        {
            var parameters = new object[] { 0, username, password, post, publish };
            var mi = this.GetMethodInfo("BeginNewPost", parameters);
            return this.BeginInvoke(mi, parameters, this, acb, null);
        }

        [XmlRpcEnd]
        public string EndNewPost(IAsyncResult iasr)
        {
            return (string)this.EndInvoke(iasr);
        }

        [XmlRpcBegin("metaWeblog.newMediaObject")]
        public IAsyncResult BeginNewMediaObject(int blogid, string username, string password, XmlRpcStruct file, AsyncCallback acb)
        {
            var parameters = new object[] { 0, username, password, file };
            var mi = this.GetMethodInfo("BeginNewMediaObject", parameters);
            return this.BeginInvoke(mi, parameters, this, acb, null);
        }

        [XmlRpcEnd]
        public XmlRpcStruct EndNewMediaObject(IAsyncResult iasr)
        {
            return (XmlRpcStruct)this.EndInvoke(iasr);
        }

        [XmlRpcBegin("metaWeblog.editPost")]
        public IAsyncResult BeginEditPost(int postid, string username, string password, XmlRpcStruct post, bool publish, AsyncCallback acb)
        {
            var parameters = new object[] { postid, username, password, post, publish };
            var mi = this.GetMethodInfo("BeginEditPost", parameters);
            return this.BeginInvoke(mi, parameters, this, acb, null);
        }

        [XmlRpcEnd]
        public bool EndEditPost(IAsyncResult iasr)
        {
            return (bool)this.EndInvoke(iasr);
        }
    }
   
}
