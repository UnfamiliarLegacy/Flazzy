﻿namespace Flazzy.ABC.AVM2.Instructions;

public class IncrementIns : ASInstruction
{
    public IncrementIns()
        : base(OPCode.Increment)
    { }

    public override int GetPopCount()
    {
        return 1;
    }
    public override int GetPushCount()
    {
        return 1;
    }
    public override void Execute(ASMachine machine)
    {
        object result = null;
        object value = machine.Values.Pop();
        if (value != null)
        {
            result = (Convert.ToDouble(value) + 1);
        }
        machine.Values.Push(result);
    }
}