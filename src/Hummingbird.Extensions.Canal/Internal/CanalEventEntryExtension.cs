using Com.Alibaba.Otter.Canal.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hummingbird.Extensions.Canal.Extensions
{
    internal static class CanalEventEntryExtension
    {
        public static CanalEventEntry ToCanalEventEntry(this Entry entry)
        {
            var cdc = new CanalEventEntry();
            cdc.position = entry.Header.LogfileName;
            cdc.xoffset = entry.Header.LogfileOffset;
            cdc.database = entry.Header.SchemaName;
            cdc.table = entry.Header.TableName;
            cdc.ts = entry.Header.ExecuteTime;
            cdc.gtid = entry.Header.Gtid;
            //获取行变更
            var rowChange = RowChange.Parser.ParseFrom(entry.StoreValue);

            if (rowChange != null)
            {              
                cdc.type = rowChange.EventType.ToString().ToLower();
                //输出 insert/update/delete 变更类型列数据
                foreach (var rowData in rowChange.RowDatas)
                {
                    if (rowChange.EventType == EventType.Insert)
                    {
                        cdc.data = new Dictionary<string, dynamic>();

                        foreach (var column in rowData.AfterColumns)
                        {
                            cdc.data.Add(column.Name, column.Value);
                        }
                    }
                    else if (rowChange.EventType == EventType.Delete)
                    {
                        cdc.old = new Dictionary<string, dynamic>();

                        foreach (var column in rowData.BeforeColumns)
                        {
                            cdc.old.Add(column.Name, column.Value);
                        }
                    }
                    else if (rowChange.EventType == EventType.Update)
                    {
                        cdc.data = new Dictionary<string, dynamic>();
                        cdc.old = new Dictionary<string, dynamic>();
                        foreach (var column in rowData.AfterColumns)
                        {
                            cdc.data.Add(column.Name, column.Value);
                        }

                        foreach (var column in rowData.BeforeColumns)
                        {
                            cdc.old.Add(column.Name, column.Value);
                        }

                    }
                }

              
            }

            return cdc;
        }

    }
}
