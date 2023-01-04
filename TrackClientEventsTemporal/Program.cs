﻿using TrackClientEventsTemporal;

await using var dbcontext = new ClientDbContext();
await dbcontext.Database.EnsureDeletedAsync();
await dbcontext.Database.EnsureCreatedAsync();
Console.WriteLine("New record added in History table");
var newClient = new Client
{
    Name = "Client 5",
    Status = ClientStatus.Active,
    Birthday = DateTime.Today
};
await dbcontext.Client.AddAsync(newClient);
await dbcontext.SaveChangesAsync();
Console.ReadLine();
Console.WriteLine("new record with updated name added to History table");
newClient.Name = "Client 6";
dbcontext.Client.Update(newClient);
await dbcontext.SaveChangesAsync();
Console.ReadLine();
Console.WriteLine("Status updated. new Record added in History table");
newClient.Status = ClientStatus.Inactive;
dbcontext.Client.Update(newClient);
await dbcontext.SaveChangesAsync();
Console.ReadLine();
Console.WriteLine("birthday is private data and stored in History Table as NULL");
newClient.Birthday = new DateTime(2000, 11, 10);
dbcontext.Client.Update(newClient);
await dbcontext.SaveChangesAsync();
Console.ReadLine();