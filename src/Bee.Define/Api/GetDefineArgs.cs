﻿using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 取得定義資料的傳入引數。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class GetDefineArgs : BusinessArgs
    {
        /// <summary>
        /// 定義資料類型。
        /// </summary>
        [Key(100)]
        public DefineType DefineType { get; set; }

        /// <summary>
        /// 取得定義資料的鍵值。
        /// </summary>
        [Key(101)]
        public string[] Keys { get; set; } = null;  
    }
}
