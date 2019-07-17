using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PureMVC.Interfaces;
using PureMVC.Patterns.Command;

public class TeamplateCommand : SimpleCommand 
{
    public override void Execute(INotification notification)
    {
        base.Execute(notification);
    }
}
