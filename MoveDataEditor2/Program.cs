// See https://aka.ms/new-console-template for more information


//It's just occured to me that we should probably be using the ep file for this
//hm, actually we need to use both the data and ep files, huh
//maybe we can make it depend on how large the value entered is?
//also, we should probably ask the user if they want to first copy their rom to a new directory before editing
//that way no one unintentionally overwrites their rom

string DataFilePath;
Console.WriteLine("Enter Data File Path");
DataFilePath = Console.ReadLine();

if (DataFilePath == null)
{
    Console.WriteLine("null value entered. Why would you do that?");
    return;
}

DataFilePath = DataFilePath.Trim('"');
Console.WriteLine(DataFilePath);





DataFilePath = Path.GetFullPath(DataFilePath);
if (!File.Exists(DataFilePath))
{
    Console.WriteLine("Invalid path entered");
    return;
}


using (FileStream fs = File.Open(DataFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
{
    //set the filestream position to the beginning of the move data table. Well, the beginning +4...
    //for now we're skipping the first 4 bytes that tell us how big the table is, but I'll probably change this later
    //need to fix the 0 indexing
    fs.Seek(0x120004, SeekOrigin.Begin);

    //somewhere here we'll have the user enter a move ID and we'll advance in the list by the ID multiplied by 4
    //We should probably account for both hex and decimal IDs
    //0 index might be an issue too? not sure

    string MoveIDEntered;
    Console.WriteLine("Enter the ID of the move who's data you wish to access");
    MoveIDEntered = Console.ReadLine();

    if (MoveIDEntered == null)
    {
        Console.WriteLine("null value entered. Why would you do that?");
        return;
    }


    try
    {
        int MoveIDForSeeking = Convert.ToInt32(MoveIDEntered);
        int CalculatedOffset = MoveIDForSeeking * 4;
        if (CalculatedOffset < 0)
        {
            Console.WriteLine($"Negative ID entered, try entering a move ID that exists/is positive");
        }
        fs.Seek(CalculatedOffset + 0x120004, SeekOrigin.Begin);
    }
    catch (Exception e)
    {
        Console.WriteLine(e.ToString());
        return;
    }


    //Read the first 3 bytes of the address, as we don't ever need the fourth
    byte[] AddressBuffer = new byte[3];
    fs.ReadExactly(AddressBuffer, 0, 3);

    //This just reads the address to the Console so we can verify that it's working
    string HexBuffer = Convert.ToHexString(AddressBuffer);
    Console.WriteLine(HexBuffer);
    //IT WORKS YEAHHHHHHHHHHHHH


    //Reverse the bytes to compensate for endianness
    Array.Reverse(AddressBuffer);


    //I wanted to use the hex version of the AddressBuffer as an offset, but I couldn't figure it out
    //so this'll have to do

    HexBuffer = Convert.ToHexString(AddressBuffer);

    int FinalOffset = int.Parse(HexBuffer, System.Globalization.NumberStyles.HexNumber);
    Console.WriteLine(FinalOffset);

    //Jump to the offset, and get the animation length in frames
    fs.Seek(FinalOffset, SeekOrigin.Begin);
    byte[] DataBuffer = new byte[2];
    fs.ReadExactly(DataBuffer, 0, 2);

    //For some reason we don't need to reverse the bytes??? why?????????
    //Array.Reverse(DataBuffer);

    int MoveLength = BitConverter.ToInt16(DataBuffer, 0);
    Console.WriteLine($"This animation is {MoveLength} frames long!");

    fs.Seek(FinalOffset + 4, SeekOrigin.Begin);

    //byte[] FloatByteBuffer = new byte[4];

    byte[] FloatBuffer = new byte[4];

    //byte[] DefaultFloat = [0x00, 0x00, 0x80, 0x3F];

    float ConvertedBuffer = BitConverter.ToSingle(FloatBuffer, 0);

    int counter = 0;
    //while (counter < 15)

    while (ConvertedBuffer != 1)
    {


        fs.ReadExactly(FloatBuffer, 0, 4);
        ConvertedBuffer = BitConverter.ToSingle(FloatBuffer, 0);
        string FloatBufferString = Convert.ToHexString(FloatBuffer);
        Console.WriteLine(FloatBufferString);
        float FBuffer = BitConverter.ToSingle(FloatBuffer, 0);
        Console.WriteLine(FBuffer);


        //counter++;
    }

    //Move the Filestream position back 4 bytes so we include the first keyframe in the list
    fs.Position = (fs.Position - 4);
    List<int> UniqueKeyFrames = new List<int>();

    //the while condition here runs unless the float buffer returns a non-integer value
    //the check against float.Epsilon accounts for miniscule rounding errors
    while ((ConvertedBuffer % 1) < float.Epsilon)
    {
        fs.ReadExactly(FloatBuffer, 0, 4);
        ConvertedBuffer = BitConverter.ToSingle(FloatBuffer, 0);
        if (ConvertedBuffer == 1)
        {
            //just keeping track of how many "sets" of keyframes there are
            ++counter;
        }

        //following the keyframe list, there's typically some bytes that are equal to very large/small integers
        //by checking the float's absolute value against 500, we ensure we aren't accidentally reading those
        //logically, no move should ever be 500 frames long, so this solution shouldn't be an issue
        //there's definitely a better way to do this though
        if (Math.Abs(ConvertedBuffer) > 500)
        {
            break;
        }

        int BufferToInt = (int)ConvertedBuffer;
        
        //here we're making a list of the unique keyframes
        //in the future the user will be able to view them and will have the option to replace them
        //this isn't super customizable but it's good for simple framedata changes
        if (!UniqueKeyFrames.Contains(BufferToInt))
        {
        UniqueKeyFrames.Add(BufferToInt);
        }


    }

    //string DefaultF = BitConverter.ToString(DefaultFloat);
    //Console.WriteLine(DefaultF);

    
    UniqueKeyFrames.ForEach(i => Console.WriteLine(i + "," ));
    //Console.WriteLine($"Found the first float!!!!!");

}