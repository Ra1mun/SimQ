using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimQCore
{
    public class ErrorMessage
    {
        //Строка для записи ошибок, происходящих в ходе вычисления оценки распределения
        //Можно накапливать все возникающие ошибки, в одну строку
        //Если строка пустая, то ошибок не было
        public string ErrorMsg = string.Empty;


        public string Add_ErrorMsg(string errorMsg, bool fromNewLine = false, string delimiter = " ")
        {
            if (fromNewLine)
                return ErrorMsg = "\n" + errorMsg + delimiter + ErrorMsg;
            else
                return ErrorMsg = errorMsg + delimiter + ErrorMsg;
        }
    }




    public struct Type_Name(string type, string name = "")
    {
        public string Type = type;
#nullable enable
        public string? Name = name;
    }

}
