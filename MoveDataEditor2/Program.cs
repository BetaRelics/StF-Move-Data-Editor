
//we need to use both the data and ep files, huh
//maybe we can make it depend on how large the value entered is?
Start:
string RomPath;
string DataFilePath;
string EpFilePath;
string Code1Path;
Console.WriteLine($"Just a heads up, this application will make a back-up of your current rom folder");
Console.WriteLine($"It'll be stored in a folder in the same directory as this application");
Console.WriteLine($"I'd keep in mind whether your stf_rom folder is modded or not");
Console.WriteLine($"Enter stf_rom folder Path");
Console.WriteLine(Directory.GetCurrentDirectory());
RomPath = Console.ReadLine();

if (RomPath == null)
{
    Console.WriteLine("null value entered. Why would you do that?");
    return;
}

//trims quotes from path if there are any
RomPath = RomPath.Trim('"');
Console.WriteLine(RomPath);

//Console.WriteLine($"Do you wish to make a copy of the original folder before you make any changes?");
//if (Console.ReadLine() == "y")

string FolderForBackup = Path.Combine(Directory.GetCurrentDirectory(), $"TempRomBackUp");
var dir = new DirectoryInfo(RomPath);
if (!dir.Exists)
{
    Console.WriteLine($"The Entered folder doesn't seem to exist, here's what was entered:{dir.FullName}");
    Console.WriteLine($"Press any key and hit enter to start the program from the beginning again");
    Console.ReadLine();
    goto Start;
}

DirectoryInfo[] dirs = dir.GetDirectories();
Directory.CreateDirectory(FolderForBackup);
foreach (FileInfo filein in dir.GetFiles())
{
    string TargetFilePath = Path.Combine(FolderForBackup, filein.Name);
    filein.CopyTo( TargetFilePath, true);
}

DataFilePath = Path.Combine(RomPath, $"rom_data.bin");
EpFilePath = Path.Combine(RomPath, $"rom_ep.bin");
Code1Path = Path.Combine(RomPath, $"rom_code1.bin");


DataFilePath = Path.GetFullPath(DataFilePath);
if (!File.Exists(DataFilePath))
{
    Console.WriteLine("Invalid path entered");
    return;
}


using (FileStream fs = File.Open(DataFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
{
    //set the filestream position to the beginning of the move data table. Well, the beginning +4...
    //for now we're skipping the first 4 bytes that tell us how big the table is, but I'll probably change this later
    //need to fix the 0 indexing
    fs.Seek(0x120004, SeekOrigin.Begin);

    //somewhere here we'll have the user enter a move ID and we'll advance in the list by the ID multiplied by 4
    //We should probably account for both hex and decimal IDs
    //0 index might be an issue too? not sure

    string MoveIDEntered;
    int MoveIDForSeeking;
    int CalculatedOffset;
    Console.WriteLine("Enter the ID of the move who's data you wish to access");
    MoveIDEntered = Console.ReadLine();

    if (MoveIDEntered == null)
    {
        Console.WriteLine("null value entered. Why would you do that?");
        return;
    }


    try
    {
        MoveIDForSeeking = Convert.ToInt32(MoveIDEntered);
        CalculatedOffset = MoveIDForSeeking * 4;
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
    //string HexBuffer = Convert.ToHexString(AddressBuffer);
    //Console.WriteLine(HexBuffer);
    //IT WORKS YEAHHHHHHHHHHHHH


    //Reverse the bytes to compensate for endianness
    Array.Reverse(AddressBuffer);


    //I wanted to use the hex version of the AddressBuffer as an offset, but I couldn't figure it out
    //so this'll have to do

    string HexBuffer = Convert.ToHexString(AddressBuffer);

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

        //somewhere here we might include some code that checks for how many keyframes are in each set
        //since there are bytes right before the keyframe floats that specify that
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
    long FloatStartPos = fs.Position;
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
            //possibly changes this to counting the bytes that defines amount of keyframes
            ++counter;
        }

        //following the keyframe list, there's typically some bytes that are equal to very large/small integers
        //by checking the float's absolute value against 400, we ensure we aren't accidentally reading those
        //logically, no move should ever be 400 frames long, so this solution shouldn't be an issue
        //there's definitely a better way to do this though
        //This can be updated due to the new findings in the "header" part
        if (Math.Abs(ConvertedBuffer) > 400)
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


    UniqueKeyFrames.ForEach(i => Console.WriteLine(i + ","));
    List<int> NewKeyFramesList = new List<int>();
    //string NewList;
    int CurrentKeyFrame = 1;
    Console.WriteLine($"Please enter what you'd like to swap these frame timings to");
    Console.WriteLine($"Enter one number at a time, in the order they appeared in before");
    Console.WriteLine($"Keep in mind entering the same number for multiple frames will result in-");
    Console.WriteLine($"-there being fewer unique keyframes, which may not be ideal");
    //Console.WriteLine($"Make sure the);

    //This is the part that allows the user to make a list of their own keyframes
    foreach (int KeyFrame in UniqueKeyFrames)
    {
        try
        {
            Console.WriteLine($"Enter keyframe {CurrentKeyFrame}");
            string NewKeyFrame = Console.ReadLine();
            if (NewKeyFrame == null)
            {
                return;
            }
            int KeyFrameForReplace = Convert.ToInt16(NewKeyFrame);
            NewKeyFramesList.Add(KeyFrameForReplace);
            ++CurrentKeyFrame;
        }

        catch
        {
            Console.WriteLine($"Something went wrong, failed to read the entered keyframe");
            Console.WriteLine($"Try again");
            NewKeyFramesList.Remove(CurrentKeyFrame);
            --CurrentKeyFrame;
        }
    }
    //int OldListLength = UniqueKeyFrames.Count;
    //int NewListLength = NewKeyFrames.Count;
    if (UniqueKeyFrames.Count - NewKeyFramesList.Count != 0)
    {
        Console.WriteLine($"Error! List lengths do not match!");
        return;
    }

    ConvertedBuffer = 0;
    CurrentKeyFrame = 0;
    

    
    foreach (int NewKeyFrameForReplace in NewKeyFramesList)
    {
        try
        {
            //Resets the position for each int we search for
            //Resets the buffer so it doesn't get stuck
            fs.Position = FloatStartPos;
            ConvertedBuffer = 1;
            while ((ConvertedBuffer % 1) < float.Epsilon)
            {
                //Reads a float and stores it
                fs.ReadExactly(FloatBuffer, 0, 4);
                ConvertedBuffer = BitConverter.ToSingle(FloatBuffer, 0);

                //Gets the float equivalent of a certain int in each list
                //Need to rename these
                var piss = UniqueKeyFrames[CurrentKeyFrame];
                float pissfloat = Convert.ToSingle(piss);
                byte[] pissbyte = BitConverter.GetBytes(pissfloat);
                var shit = NewKeyFramesList[CurrentKeyFrame];
                float shitfloat = Convert.ToSingle(shit);
                byte[] shitbyte = BitConverter.GetBytes(shitfloat);
                Console.WriteLine(piss);
                
                //Hits if the float matches one of the original keyframes
                if (ConvertedBuffer == pissfloat)
                {
                    //string test = BitConverter.ToString(pissbyte);
                    //Console.WriteLine(test);

                    //Overwrites the float with the user entered float from the new list
                    fs.Position = (fs.Position - 4);
                    fs.Write(shitbyte, 0, 4);
                    
                    //debug
                    string shitstring = Convert.ToHexString(shitbyte);
                    Console.WriteLine(shitstring);

                    //UniqueKeyFrames.ForEach(i => {Console.WriteLine(i.ToString());});

                }
                //Should only hit when the stream goes past the floats
                //This can be updated with more accurate code due to the new findings in the "header" part
                if (ConvertedBuffer > 400)
                {
                    Console.WriteLine($"Parsed value too big!!!");
                    ++CurrentKeyFrame;
                    break;
                    //I'm so fucking stupid
                    //ConvertedBuffer = 1;
                }
                //debug
                Console.WriteLine($"loop");
            }
            
        }
        catch (Exception e)
        {
            //debug
            Console.WriteLine(e.ToString());
            Console.WriteLine($"shit");
        }

        

    }

    Console.WriteLine($"Please enter what frame the animation ends on");
    Console.WriteLine($"(I recommend just making it the highest value you entered");
    int EndingFrame = Convert.ToInt16( Console.ReadLine() );
    fs.Position = FinalOffset;
    byte[] EndingFrameBytes = new byte[2];
    EndingFrameBytes = BitConverter.GetBytes(EndingFrame);
   
    //Commented-out debug code.
    //string EndF = Convert.ToHexString(EndingFrameBytes);
    //Console.WriteLine(EndF);
    
    fs.Write(EndingFrameBytes, 0, 2);

    //Now we'll write some values to the code1 file so things work smoothly
    //Disposing the original filestream, as it should no longer be needed?
    fs.Dispose();
    using (FileStream Code1fs = File.Open(Code1Path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
    {
        Code1fs.Seek(CalculatedOffset + 0xCE380, 0);
        Code1fs.ReadExactly(AddressBuffer, 0, 3);
        //string hexbuff = Convert.ToHexString(AddressBuffer);
        //Console.WriteLine(hexbuff);
        
        //We have to reverse the endianness again
        Array.Reverse(AddressBuffer);

        //I have no idea if we have to convert it to a hex string first, it's just how I did it before
        HexBuffer = Convert.ToHexString(AddressBuffer);
        FinalOffset = int.Parse(HexBuffer, System.Globalization.NumberStyles.HexNumber);
        if (FinalOffset == 0xFFFFFF) 
        {
            return;
        }
        if (MoveIDForSeeking == 0)
        {
            return;
        }
        Code1fs.Seek(FinalOffset, 0);

        //Active Frame Start (if the move has active frames)
        Code1fs.Position = Code1fs.Position + 14;
        byte[] intbuffer = new byte[2];
        //this works
        //Code1fs.ReadExactly(intbuffer, 0, 2);
        Code1fs.ReadExactly(intbuffer, 0, 2);
        Code1fs.Position = Code1fs.Position - 2;
        Console.WriteLine($"Enter what frame you want the move to become active on");
        Console.WriteLine($"Original value was {BitConverter.ToInt16(intbuffer)}");
        string StringActFrame = Console.ReadLine();
        if (StringActFrame == null)
        {
            return;
        }
        int IntActFrame = Convert.ToInt16(StringActFrame);
        byte[] ByteActFrame = BitConverter.GetBytes(IntActFrame);
        Code1fs.Write(ByteActFrame, 0, 2);




        Code1fs.ReadExactly(intbuffer, 0, 2);
        Code1fs.Position = Code1fs.Position - 2;
        Console.WriteLine($"Enter what frame you want the move to stop being active on");
        Console.WriteLine($"Original value was {BitConverter.ToInt16(intbuffer)}");
        StringActFrame = Console.ReadLine();
        if (StringActFrame == null)
        {
            return;
        }
        IntActFrame = Convert.ToInt16(StringActFrame);   
        ByteActFrame = BitConverter.GetBytes(IntActFrame);
        Code1fs.Write(ByteActFrame, 0, 2);


        Code1fs.ReadExactly(intbuffer, 0, 2);
        Code1fs.Position = Code1fs.Position - 2;
        Console.WriteLine($"Enter the frame the move ends on");
        Console.WriteLine($"Original value was {BitConverter.ToInt16(intbuffer)}");
        StringActFrame = Console.ReadLine();
        if (StringActFrame == null)
        {
            Console.WriteLine($"Null value entered, don't do that");
            return;
        }
        IntActFrame = Convert.ToInt16(StringActFrame);
        ByteActFrame = BitConverter.GetBytes(IntActFrame);
        Code1fs.Write(ByteActFrame, 0, 2);

    }


    fs.Close();

}
    