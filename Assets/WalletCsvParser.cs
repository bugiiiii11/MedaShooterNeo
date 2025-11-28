using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WalletCsvParser : Singleton<WalletCsvParser>
{
    public TextAsset csvFile; // Reference of CSV file

    public List<string> allowedAddresses;

    private void OnEnable()
    {
        allowedAddresses = ReadWalletAddressData();
    }

    public bool IsAllowed(string userAddress)
    {
        return allowedAddresses.Contains(userAddress);
    }

    public List<string> ReadWalletAddressData()
    {
        string[] records = csvFile.text.ToLower().Split('\n');
        
        for(var i = 0; i < records.Length; i++)
        {
            records[i] = new string(records[i].Where(c => !char.IsWhiteSpace(c)).ToArray());
        }

        return records[..^1].ToList();

        //foreach (string record in records)
        //{
        //    string[] fields = record.Split(fieldSeperator);
        //    foreach (string field in fields)
        //    {
        //        contentArea.text += field + "\t";
        //    }
        //    contentArea.text += '\n';
        //}
    }
}
