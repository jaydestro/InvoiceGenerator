namespace Invoice
{
    public class Item
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal ShippingCost { get; set; }

        public Item(string name)
        {
            Name = name;
            Price = 0M;
            Quantity = 0;
            ShippingCost = 0M;
        }
    }
}
