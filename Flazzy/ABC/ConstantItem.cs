namespace Flazzy.ABC
{
    public abstract class ConstantItem : FlashItem
    {
        public ASConstantPool Pool { get; }

        public ConstantItem(ASConstantPool pool)
        {
            Pool = pool;
        }
    }
}
