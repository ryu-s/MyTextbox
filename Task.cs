using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyLibrary
{
    /// <summary>
    /// ReDo, UnDoの実装用
    /// </summary>
    public class Task
    {
        public enum Type
        {
            DELETE,
            INSERT,
            REPLACE,
            REPLACEALL,// 一つだけ戻すだけでなく、全部戻すこともできるようにする。


        }
        public Type type;
        public int fromLine;
        public int fromPos;
        public int length;
        public string str;
    }
    /// <summary>
    /// 
    /// </summary>
    public class ReplaceAll : Task
    {
        public ReplaceAll()
        {
            type = Type.REPLACEALL;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class Replace : Task
    {
        public Replace()
        {
            type = Type.REPLACE;
        }
    }

    public class Insert : Task
    {
        public Insert()
        {
            type = Type.INSERT;
        }
    }
    public class Delete : Task
    {
        public Delete()
        {
            type = Type.DELETE;
        }
    }
}
