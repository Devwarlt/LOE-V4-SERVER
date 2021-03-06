﻿#region

using common;
using System;
using System.Collections.Generic;
using System.Linq;
using static appengine.package.getPackages;

#endregion

namespace appengine.account
{
    internal class purchasePackage : RequestHandler
    {
        protected override void HandleRequest()
        {
            DbAccount acc;
            if (Query["guid"] == null || Query["password"] == null)
                WriteErrorLine("Error.incorrectEmailOrPassword");
            else
            {
                LoginStatus status = Database.Verify(Query["guid"], Query["password"], out acc);
                if (status == LoginStatus.OK)
                {
                    if (Query["packageId"] == null)
                    {
                        WriteLine("<Error>Package ID not declared.</Error>");
                        return;
                    }

                    SerializePackageResponse package = SerializePackageResponse.GetPackage(int.Parse(Query["packageId"]));

                    if (package == null)
                    {
                        WriteLine($"<Error>Package ID {Query["packageId"]} not found.</Error>");
                        return;
                    }

                    if (acc.Credits < package.Price)
                    {
                        WriteLine("<Error>Not enough gold.</Error>");
                        return;
                    }

                    if (Database.CheckPackage(acc, package.PackageId, package.MaxPurchase))
                    {
                        WriteLine($"<Error>You can only purchase {package.Name} {package.MaxPurchase} time{((package.MaxPurchase <= 1) ? "" : "s")}.</Error>");
                        return;
                    }

                    if (package.MaxPurchase != -1)
                        Database.AddPackage(acc, package.PackageId);

                    Database.UpdateCredit(acc, -package.Price);

                    int[] gifts = Utils.FromCommaSepString32(package.Contents);

                    List<int> giftsList = acc.Gifts.ToList();

                    foreach (int item in gifts)
                        giftsList.Add(item);

                    acc.Gifts = giftsList.ToArray();

                    acc.Flush();
                    acc.Reload();

                    WriteLine("<Success/>");
                }
                else
                    WriteErrorLine(status.GetInfo());
            }
        }

        private int[] GetAwards(string items)
        {
            Random rand = new Random();
            int[] ret = new int[items.Split(',').Length];
            //ret[0] = Utils.FromString();
            for (int i = 0; i < ret.Length; i++)
                ret[i] = Utils.FromString(items.Split(';')[0].Split(',')[rand.Next(items.Split(';')[0].Split(',').Length)]);
            return ret.ToArray();
        }

        struct PackageContent
        {
            public List<int> items;
            public int vaultChests;
            public int charSlots;
        }
    }
}