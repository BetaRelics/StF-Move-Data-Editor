using System.Text;
Start:
string RomPath;
string DataFilePath;
string EpFilePath;
string Code1Path;
int ChangeBy = 0;
bool ChangeAll = false;
List<int> AlreadyWrittenTo = new List<int>();
Console.WriteLine($"Just a heads up, this application will make a back-up of your current rom folder");
Console.WriteLine($"It'll be stored in a folder in the same directory as this application");
Console.WriteLine($"I'd keep in mind whether your stf_rom folder is modded or not");
Console.WriteLine($"Enter stf_rom folder Path");
//Console.WriteLine(Directory.GetCurrentDirectory());
RomPath = Console.ReadLine();

if (String.IsNullOrWhiteSpace(RomPath))
{
    Console.WriteLine("null value entered.");
    Console.WriteLine($"Starting the program from the beginning...");
    goto Start;
}

//trims quotes from path if there are any
RomPath = RomPath.Trim('"');
//Console.WriteLine(RomPath);

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

if (Directory.Exists(FolderForBackup))
{
    Console.WriteLine($"A backup folder was found, do you wish to overwrite it? type 'y' for yes, or 'n' for no.");
    Console.WriteLine($"If you wish to restore the backup, type 'r' and hit enter");
    string PathResponse = Console.ReadLine();
    if (String.IsNullOrWhiteSpace(PathResponse))
    {
        Console.WriteLine($"Null value entered, restarting the program");
        goto Start;
    }
    if (PathResponse == "n")
    {
        goto PathCombining;
    }
    else if (PathResponse == "y")
    {
        goto CreateBackup;
    }
    else if (PathResponse == "r")
    {
        var backdir = new DirectoryInfo(FolderForBackup);
        foreach (FileInfo BackFile in backdir.GetFiles())
        {
            File.Copy(BackFile.FullName, Path.Combine(RomPath, BackFile.Name), true);
            goto PathCombining;
        }
    }
    else
    {
        Console.WriteLine($"Reponse not recognized, did you enter lowercase 'y' , 'n' or 'r'?");
        Console.WriteLine($"Press any key and hit enter to return to the start of the program");
        Console.ReadLine();
        goto Start;
    }
}

CreateBackup:
DirectoryInfo[] dirs = dir.GetDirectories();
Directory.CreateDirectory(FolderForBackup);
foreach (FileInfo filein in dir.GetFiles())
{
    string TargetFilePath = Path.Combine(FolderForBackup, filein.Name);
    filein.CopyTo( TargetFilePath, true);
}

PathCombining:
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
    //for now we're skipping the first 4 bytes that tell us how big the table is
    fs.Seek(0x120004, SeekOrigin.Begin);

    string HexQuery;
    bool HexMode = false;
    string MoveIDEntered;
    int MoveIDForSeeking;
    int CalculatedOffset;
    Console.WriteLine($"Before we start, do you wish to enter the move ID in hex? type 'y' for yes, or 'n' for no, and hit enter");
    HexQuery = Console.ReadLine();
    if (String.IsNullOrWhiteSpace(HexQuery))
    {
        Console.WriteLine("No response detected, proceeding with Decimal Mode");
    }
    else if (HexQuery == "y")
    {
        Console.WriteLine($"Hex Mode enabled");
        HexMode = true;
    }
    else if (HexQuery == "n")
    {
        Console.WriteLine($"Proceeding with Decimal Mode");
        HexMode = false;
    }
    else
    {
        Console.WriteLine("Invalid Response, Restarting the program");
        goto Start;
    }
    RequestID:
    Console.WriteLine("Enter the ID of the move whose data you wish to access");
    MoveIDEntered = Console.ReadLine();

    if (String.IsNullOrWhiteSpace(MoveIDEntered))
    {
        Console.WriteLine("No ID detected, try entering the ID again");
        goto RequestID;
    }
   

    try
    {
        if (HexMode == true)
        {
            MoveIDForSeeking = int.Parse(MoveIDEntered, System.Globalization.NumberStyles.HexNumber);
        }
        else
        {
            MoveIDForSeeking = Convert.ToInt32(MoveIDEntered);
        }

        if (MoveIDForSeeking == 0)
        {
            Console.WriteLine($"ID 0 is assigned to NULL, and thus is not a valid move");
            goto RequestID;
        }
        if (MoveIDForSeeking > 517)
        {
            Console.WriteLine($"The ID entered is outside the range of valid moves (517 is the cutoff)");
            goto RequestID;
        }
        if (MoveIDForSeeking > 458) 
        {
            goto EpFileProcessing;
        }
        CalculatedOffset = MoveIDForSeeking * 4;
        if (CalculatedOffset < 0)
        {
            Console.WriteLine($"Negative ID entered, try entering a move ID that exists/is positive");
            goto RequestID;
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
    //Console.WriteLine(FinalOffset);

    //Jump to the offset, and store the position.
    fs.Seek(FinalOffset, SeekOrigin.Begin);
    var DataPos = fs.Position;
    //Console.WriteLine($"{FinalOffset} {DataPos}");
    //Go to the internal name table and find the address of the move's name
    byte[] NameAddress = new byte[3];
    fs.Seek(CalculatedOffset + 0x120738, SeekOrigin.Begin);
    fs.ReadExactly(NameAddress, 0, 3);
    Array.Reverse(NameAddress);
    string NameHexBuffer = Convert.ToHexString(NameAddress);
    
    int FinalNameOffset = int.Parse(NameHexBuffer, System.Globalization.NumberStyles.HexNumber);
    //Jump to said address
    fs.Seek(FinalNameOffset, SeekOrigin.Begin);
    
    //Read bytes till we hit the terminator to get the length of the name
    bool NameTerminatorCheck = false;
    int NameLength = 0;
    while (NameTerminatorCheck == false)
    {
        int NameByte = fs.ReadByte();
        if (NameByte == 0)
        {
            //Console.WriteLine(NameByte);
            //Console.WriteLine(fs.Position);
            NameTerminatorCheck = true;
            break;
        }
       
        ++NameLength;
    }
    //Now that we know the length, read the entire name to the console
    fs.Seek(FinalNameOffset, SeekOrigin.Begin);
    byte[] NameAllBytes = new byte[NameLength];
    fs.ReadExactly(NameAllBytes, 0, NameLength);
    string Name = Encoding.UTF8.GetString(NameAllBytes);
    Console.WriteLine($"This animation is named {Name}");
    //Console.WriteLine(FinalNameOffset);
    Console.WriteLine($"This animation is located at 0x{FinalOffset:X} in rom_data");
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
        //Console.WriteLine(FloatBufferString);
        float FBuffer = BitConverter.ToSingle(FloatBuffer, 0);
        //Console.WriteLine(FBuffer);


        //counter++;
    }

    //Move the Filestream position back 4 bytes so we include the first keyframe in the list
    fs.Position = (fs.Position - 4);
    long FloatStartPos = fs.Position;
    //Console.WriteLine($"{FloatStartPos} {fs.Position}");
    List<int> KeyFrameCounter = new List<int>();
    List<int> UniqueKeyFrames = new List<int>();

    //The while condition here runs unless the float buffer returns a non-integer value
    //The check against float.Epsilon accounts for miniscule rounding errors
    while ((ConvertedBuffer % 1) < float.Epsilon)
    {
        fs.ReadExactly(FloatBuffer, 0, 4);
        ConvertedBuffer = BitConverter.ToSingle(FloatBuffer, 0);
        if (ConvertedBuffer == 1)
        {
            //just keeping track of how many "sets" of keyframes there are
            //possibly change this to counting the bytes that defines amount of keyframes
            ++counter;
        }

        //following the keyframe list, there's typically some bytes that are equal to very large/small integers
        //by checking the float's absolute value against 600, we ensure we aren't accidentally reading those
        //logically, no move should ever be 600 frames long, so this solution shouldn't be an issue
        //there's definitely a better way to do this though
        //We also check to make sure the float is a whole number that isn't negative 
        //This can be updated due to the new findings in the "header" part
        if (Math.Abs(ConvertedBuffer) > 600 || ConvertedBuffer < 0 || ConvertedBuffer % 1 != 0)
        {
            break;
        }
        if (Math.Abs(ConvertedBuffer) == 0)
        {
            break;
        }
        int BufferToInt = (int)ConvertedBuffer;

        //here we're making a list of the unique keyframes
        //this isn't super customizable but it's good for simple framedata changes
        if (!UniqueKeyFrames.Contains(BufferToInt))
        {
            UniqueKeyFrames.Add(BufferToInt);
        }


    }

    //string DefaultF = BitConverter.ToString(DefaultFloat);
    //Console.WriteLine(DefaultF);

    KeyFrameStuff:
    Console.WriteLine($"This animation has {UniqueKeyFrames.Count} unique Key Frames");
    Console.WriteLine($"These are the unique Key Frame values:");
    UniqueKeyFrames.ForEach(i => Console.WriteLine(i + ","));
    List<int> NewKeyFramesList = new List<int>();
    //string NewList;
    int CurrentKeyFrame = 1;
    ChangeBy = 0;
    ChangeAll = false;
    int KeyFrameForReplace;
    Console.WriteLine($"Please enter what you'd like to swap these frame timings to.");
    Console.WriteLine($"Enter one number at a time, in the order they appeared in before.");
    Console.WriteLine($"If you enter nothing, the keyframe's original value will be used.");
    Console.WriteLine($"If you enter 'All', 'all', or 'a', you will be prompted to enter another value.");
    Console.WriteLine($"The remaining keyframes will then be computed by adding this value to their original value.");
    //Console.WriteLine($"Make sure the);

    //This is the part that allows the user to make a list of their own keyframes
    foreach (int KeyFrame in UniqueKeyFrames)
    {
        try
        {
            if (ChangeAll == true)
            {
                //NewKeyFramesList.Clear();
                foreach (int i in UniqueKeyFrames)
                {
                    //Allows it to skip any already set keyframes
                    if (UniqueKeyFrames.IndexOf(i) < CurrentKeyFrame - 1)
                    {
                        continue;
                    }
                    //We always want to keep the first keyframe value equal to 1.
                    //This is because its both how all the other animations work,
                    //and because this program operates based on finding that first keyframe of 1
                    if (i == 1)
                    {
                        NewKeyFramesList.Add(1);
                    }
                    else
                    {
                        if ((i + ChangeBy) < 1)
                        {
                            Console.WriteLine($"One of the new computed keyframes is less than 1. Let's try again. {Environment.NewLine}");
                            goto KeyFrameStuff;
                        }
                        NewKeyFramesList.Add(i + ChangeBy);
                        //Console.WriteLine(ChangeBy);
                    }
                }
                break;
            }
            Console.WriteLine($"Enter keyframe {CurrentKeyFrame}");
            Console.WriteLine($"This keyframe's old value was {UniqueKeyFrames.ElementAt(CurrentKeyFrame - 1)}");
            //We always want to keep the first keyframe value equal to 1.
            //This is because its both how all the other animations work,
            //and because this program operates based on finding that first keyframe of 1
            if (UniqueKeyFrames.ElementAt(CurrentKeyFrame - 1) == 1 || (CurrentKeyFrame == 1))
            {
                Console.WriteLine($"It is highly recommended that you keep the first Keyframe set to 1");
            }
            string NewKeyFrame = Console.ReadLine();
            if (String.IsNullOrWhiteSpace(NewKeyFrame))
            {
                KeyFrameForReplace = UniqueKeyFrames.ElementAt(CurrentKeyFrame - 1);
                Console.WriteLine($"{KeyFrameForReplace}");
                NewKeyFramesList.Add(KeyFrameForReplace);
                ++CurrentKeyFrame;
            }
            else if (NewKeyFrame.Contains($"Restart") || NewKeyFrame.Contains($"restart"))
            {
                fs.Dispose();
                Console.WriteLine($"Restarting the program... {Environment.NewLine} {Environment.NewLine}");
                await Task.Delay(10000);
                goto Start;
            }
            else if (NewKeyFrame.Contains($"all") || NewKeyFrame.Contains($"All") || NewKeyFrame.Contains($"a"))
            {
                Console.WriteLine($"Enter what value to add to all of the remaining keyframes");
                string ChangeInt = Console.ReadLine();
                if (String.IsNullOrWhiteSpace(ChangeInt))
                {
                    KeyFrameForReplace = UniqueKeyFrames.ElementAt(CurrentKeyFrame - 1);
                    Console.WriteLine($"{KeyFrameForReplace}");
                    NewKeyFramesList.Add(KeyFrameForReplace);
                    ++CurrentKeyFrame;
                    continue;
                }
                if (CurrentKeyFrame - 1== 0)
                {
                    NewKeyFramesList.Add(1);
                    ChangeBy = Convert.ToInt16(ChangeInt);
                    ++CurrentKeyFrame;
                    ChangeAll = true;
                }
                else
                {
                    ChangeBy = Convert.ToInt16(ChangeInt);
                    KeyFrameForReplace = (UniqueKeyFrames.ElementAt(CurrentKeyFrame - 1) + ChangeBy);
                    NewKeyFramesList.Add(KeyFrameForReplace);
                    ++CurrentKeyFrame;
                    ChangeAll = true;
                    continue;
                }
            }
            else
            {
                KeyFrameForReplace = Convert.ToInt16(NewKeyFrame);
                NewKeyFramesList.Add(KeyFrameForReplace);
                ++CurrentKeyFrame;
            }
        }

        catch
        {
            Console.WriteLine($"Something went wrong, failed to read the entered keyframe");
            //Console.WriteLine($"Try again");
            //NewKeyFramesList.RemoveAt(CurrentKeyFrame - 1);
            //Console.WriteLine(NewKeyFramesList.Count);
            Console.WriteLine($"Start from the top {Environment.NewLine}");
            goto KeyFrameStuff;
        }
    }
    //int OldListLength = UniqueKeyFrames.Count;
    //int NewListLength = NewKeyFrames.Count;
    //Console.WriteLine($"Original List:{UniqueKeyFrames.Count}, New List:{NewKeyFramesList.Count}");
    if (UniqueKeyFrames.Count - NewKeyFramesList.Count != 0)
    {
        Console.WriteLine($"Error! List lengths do not match!");
        Console.WriteLine($"Original List:{UniqueKeyFrames.Count}, New List:{NewKeyFramesList.Count}");
        return;
    }

    Console.WriteLine($"Your new keyframes are:");
    NewKeyFramesList.ForEach(i => Console.WriteLine(i + ","));
    Console.WriteLine($"Are these values satisfactory? Type 'n' and hit enter if they are not. If they are satisfactory, just hit enter.");
    string Satisfaction = Console.ReadLine();
    if (Satisfaction == "n")
    {
        Console.WriteLine($"No? How about we try again then. {Environment.NewLine}");
        goto KeyFrameStuff;
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
                //Console.WriteLine(piss);

                //Hits if the float matches one of the original keyframes
                if (ConvertedBuffer == pissfloat)
                {
                    //string test = BitConverter.ToString(pissbyte);
                    //Console.WriteLine(test);



                    fs.Position = (fs.Position - 4);

                    //Checks if this location has already been written to, and skips writing if it was.
                    //If this location hasn't been written to, it gets added to a list of locations- 
                    //-that have been written to, and then the bytes are overwritten.
                    if (AlreadyWrittenTo.Contains(Convert.ToInt32(fs.Position)))
                    {
                        fs.Position += 4;
                        //Console.WriteLine(Convert.ToInt32(fs.Position));
                        continue;
                    }
                    else
                    {
                        AlreadyWrittenTo.Add(Convert.ToInt32(fs.Position));
                    }
                    //Overwrites the float with the user entered float from the new list
                    fs.Write(shitbyte, 0, 4);
                    //debug
                    //string shitstring = Convert.ToHexString(shitbyte);
                    //Console.WriteLine(shitstring);

                    

                }
                //Should only hit when the stream goes past the floats
                //This can be updated with more accurate code due to the new findings in the "header" part
                if (ConvertedBuffer > 600 || ConvertedBuffer < 0 || ConvertedBuffer % 1 != 0)
                {
                    //Console.WriteLine($"Parsed value too big!!!");
                    ++CurrentKeyFrame;
                    break;
                    //I'm so stupid
                    //ConvertedBuffer = 1;
                }
                //debug
                //Console.WriteLine($"loop");
            }
            
        }
        catch (Exception e)
        {
            //debug
            Console.WriteLine(e.ToString());
            Console.WriteLine($"shit");
        }

        

    }
    SetEndFrame:
    Console.WriteLine($"Please enter what frame the animation ends on");
    Console.WriteLine($"The animation previously ended on frame {MoveLength}");
    Console.WriteLine($"(I recommend just making it the highest value you entered, which was {NewKeyFramesList.Max()})");
    string EndingFrameString = Console.ReadLine();
    if (String.IsNullOrWhiteSpace(EndingFrameString))
    {
        EndingFrameString = NewKeyFramesList.Max().ToString();
        Console.WriteLine($"{EndingFrameString}");
        //Console.WriteLine($"Null value entered, try entering your value again.");
        //goto SetEndFrame;
    }
    int EndingFrame = Convert.ToInt16(EndingFrameString);
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

    Code1Handling:
    using (FileStream Code1fs = File.Open(Code1Path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
    {
        Code1fs.Seek(CalculatedOffset + 0xCE380, SeekOrigin.Begin);
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
        Code1fs.Seek(FinalOffset, SeekOrigin.Begin);
        Console.WriteLine($"{Environment.NewLine} {Name}'s move data is located at 0x{FinalOffset:X} in rom_code1");
        //Active Frame Start (if the move has active frames)
        Code1fs.Position = Code1fs.Position + 14;
        byte[] intbuffer = new byte[2];
        //this works
        //Code1fs.ReadExactly(intbuffer, 0, 2);
        Code1fs.ReadExactly(intbuffer, 0, 2);
        Code1fs.Position = Code1fs.Position - 2;
        ActiveFrameStart:
        Console.WriteLine($"Enter what frame you want the move to become active on");
        Console.WriteLine($"Original value was {BitConverter.ToInt16(intbuffer)}(?)");
        Console.WriteLine($"If the move you're modifying doesn't have active frames, type 'skip' and hit enter");
        string StringActFrame = Console.ReadLine();
        if (String.IsNullOrWhiteSpace(StringActFrame))
        {
            int convertedintbuffer = BitConverter.ToInt16(intbuffer);
            StringActFrame = convertedintbuffer.ToString();
            //Console.WriteLine($"Null value entered, try entering your value again.");
            //goto ActiveFrameStart;
        }
        if (StringActFrame == "skip")
        {
            goto skipped;
        }
        int IntActFrame = Convert.ToInt16(StringActFrame);
        byte[] ByteActFrame = BitConverter.GetBytes(IntActFrame);
        Code1fs.Write(ByteActFrame, 0, 2);




        Code1fs.ReadExactly(intbuffer, 0, 2);
        Code1fs.Position = Code1fs.Position - 2;
        ActiveFrameStop:
        Console.WriteLine($"Enter what frame you want the move to stop being active on");
        Console.WriteLine($"Original value was {BitConverter.ToInt16(intbuffer)}");
        StringActFrame = Console.ReadLine();
        if (String.IsNullOrWhiteSpace(StringActFrame))
        {
            int convertedintbuffer = BitConverter.ToInt16(intbuffer);
            StringActFrame = convertedintbuffer.ToString();
            //Console.WriteLine($"Null value entered, try entering your value again.");
            //goto ActiveFrameStop;
        }
        IntActFrame = Convert.ToInt16(StringActFrame);   
        ByteActFrame = BitConverter.GetBytes(IntActFrame);
        Code1fs.Write(ByteActFrame, 0, 2);


        Code1fs.ReadExactly(intbuffer, 0, 2);
        Code1fs.Position = Code1fs.Position - 2;
        EndFrame:
        Console.WriteLine($"Enter the frame the move ends on");
        Console.WriteLine($"Original value was {BitConverter.ToInt16(intbuffer)}");
        Console.WriteLine($"For reference, you set the animation to end on frame {EndingFrame}");
        StringActFrame = Console.ReadLine();
        if (String.IsNullOrWhiteSpace(StringActFrame))
        {
            int convertedintbuffer = BitConverter.ToInt16(intbuffer);
            StringActFrame = convertedintbuffer.ToString();
            //Console.WriteLine($"Null value entered, try entering your value again.");
            //goto EndFrame;
        }
        IntActFrame = Convert.ToInt16(StringActFrame);
        ByteActFrame = BitConverter.GetBytes(IntActFrame);
        Code1fs.Write(ByteActFrame, 0, 2);

        //Grab the byte that determines which parts become active, for now this is just future-proofing
        Code1fs.Position = Code1fs.Position + 1;
        Code1fs.ReadExactly(intbuffer, 0, 1);
        int ActiveParts = BitConverter.ToInt16(intbuffer);

        //Damage Time
        Code1fs.Position = Code1fs.Position + 1;
        Code1fs.ReadExactly(intbuffer, 0, 1);
        Code1fs.Position -= 1;
        //wait could I have just done -=/+= the whole time
        Damage:
        Console.WriteLine($"Enter how much damage the move should do (Always Enter it in Decimal)");
        Console.WriteLine($"Original value was {BitConverter.ToInt16(intbuffer)}");
        StringActFrame = Console.ReadLine();
        if (String.IsNullOrWhiteSpace(StringActFrame))
        {
            Console.WriteLine($"Null value entered, try entering your value again.");
            goto Damage;
        }
        IntActFrame = Convert.ToInt16(StringActFrame);
        ByteActFrame = BitConverter.GetBytes(IntActFrame);
        Code1fs.Write(ByteActFrame, 0, 1);
    }

    skipped:
    fs.Close();
    Console.WriteLine($"You're all set!");
    return;


    EpFileProcessing: 
    fs.Close();
    using (FileStream Epfs = File.Open(EpFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)) 
    { 
        Epfs.Seek(0x04, SeekOrigin.Begin);
        try 
        {
            CalculatedOffset = MoveIDForSeeking * 4;
            if (CalculatedOffset < 0)
            {
                Console.WriteLine($"Negative ID entered, try entering a move ID that exists/is positive");
            }
            if (CalculatedOffset == 0)
            {
                Console.WriteLine($"ID 0 is assigned to Null, and thus is not a valid move");
                return;
            }

            
            Epfs.Seek(CalculatedOffset + 0x04, SeekOrigin.Begin);
           
            
            //Read the first 3 bytes of the address, as we don't ever need the fourth
            AddressBuffer = new byte[3];
            Epfs.ReadExactly(AddressBuffer, 0, 3);
            
            Array.Reverse(AddressBuffer);

            HexBuffer = Convert.ToHexString(AddressBuffer);

            FinalOffset = int.Parse(HexBuffer, System.Globalization.NumberStyles.HexNumber);
            //Ep file addresses are offset by 0x400000
            FinalOffset = FinalOffset - 0x400000;
            //Console.WriteLine(FinalOffset);

            //Go to the internal name table and find the address of the move's name
            NameAddress = new byte[3];
            Epfs.Seek(CalculatedOffset + 0x820, SeekOrigin.Begin);
            Epfs.ReadExactly(NameAddress, 0, 3);
            Array.Reverse(NameAddress);
            NameHexBuffer = Convert.ToHexString(NameAddress);

            FinalNameOffset = int.Parse(NameHexBuffer, System.Globalization.NumberStyles.HexNumber);
            //Ep file addresses are offset by 0x400000
            FinalNameOffset = FinalNameOffset - 0x400000;
            //Console.WriteLine(FinalNameOffset);
            
            //Jump to said address
            Epfs.Seek(FinalNameOffset, SeekOrigin.Begin);

            //Read bytes till we hit the terminator to get the length of the name
            NameTerminatorCheck = false;
            NameLength = 0;
            while (NameTerminatorCheck == false)
            {
                int NameByte = Epfs.ReadByte();
                if (NameByte == 0)
                {
                    //Console.WriteLine(NameByte);
                    //Console.WriteLine(Epfs.Position);
                    NameTerminatorCheck = true;
                    break;
                }

                ++NameLength;
            }
            Epfs.Seek(FinalNameOffset, SeekOrigin.Begin);
            NameAllBytes = new byte[NameLength];
            Epfs.ReadExactly(NameAllBytes, 0, NameLength);
            Name = Encoding.UTF8.GetString(NameAllBytes);
            Console.WriteLine($"This animation is named {Name}");

            Console.WriteLine($"This animation is located at 0x{FinalOffset:X} in rom_ep");
            //Jump to the animation offset, and get the animation length in frames
            Epfs.Seek(FinalOffset, SeekOrigin.Begin);
            DataBuffer = new byte[2];
            Epfs.ReadExactly(DataBuffer, 0, 2);

            MoveLength = BitConverter.ToInt16(DataBuffer, 0);
            Console.WriteLine($"This animation is {MoveLength} frames long!");

            Epfs.Seek(FinalOffset + 4, SeekOrigin.Begin);

            //byte[] FloatByteBuffer = new byte[4];

            FloatBuffer = new byte[4];

            //byte[] DefaultFloat = [0x00, 0x00, 0x80, 0x3F];

            ConvertedBuffer = BitConverter.ToSingle(FloatBuffer, 0);

            counter = 0;

            try
            {

                while (ConvertedBuffer != 1)
                {

                    //somewhere here we might include some code that checks for how many keyframes are in each set
                    //since there are bytes right before the keyframe floats that specify that
                    Epfs.ReadExactly(FloatBuffer, 0, 4);
                    ConvertedBuffer = BitConverter.ToSingle(FloatBuffer, 0);
                    string FloatBufferString = Convert.ToHexString(FloatBuffer);
                    //Console.WriteLine(FloatBufferString);
                    float FBuffer = BitConverter.ToSingle(FloatBuffer, 0);
                    //Console.WriteLine(FBuffer);


                    //counter++;
                }

                //Move the Filestream position back 4 bytes so we include the first keyframe in the list
                Epfs.Position = (Epfs.Position - 4);
                FloatStartPos = Epfs.Position;
                UniqueKeyFrames = new List<int>();

                //The while condition here runs unless the float buffer returns a non-integer value
                //The check against float.Epsilon accounts for miniscule rounding errors
                while ((ConvertedBuffer % 1) < float.Epsilon)
                {
                    Epfs.ReadExactly(FloatBuffer, 0, 4);
                    ConvertedBuffer = BitConverter.ToSingle(FloatBuffer, 0);
                    if (ConvertedBuffer == 1)
                    {
                        //just keeping track of how many "sets" of keyframes there are
                        //possibly changes this to counting the bytes that defines amount of keyframes
                        ++counter;
                    }

                    //following the keyframe list, there's typically some bytes that are equal to very large/small integers
                    //by checking the float's absolute value against 600, we ensure we aren't accidentally reading those
                    //logically, no move should ever be 600 frames long, so this solution shouldn't be an issue
                    //there's definitely a better way to do this though
                    //We also check to make sure the float is a whole number that isn't negative 
                    //This can be updated due to the new findings in the "header" part
                    if (Math.Abs(ConvertedBuffer) > 600 || ConvertedBuffer < 0 || ConvertedBuffer % 1 != 0)
                    {
                        break;
                    }
                    if (Math.Abs(ConvertedBuffer) == 0)
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
                EpKeyFrameStuff:
                //UniqueKeyFrames.Sort();
                Console.WriteLine($"This animation has {UniqueKeyFrames.Count} unique Key Frames");
                Console.WriteLine($"These are the unique Key Frame values:");
                UniqueKeyFrames.ForEach(i => Console.WriteLine(i + ","));
                NewKeyFramesList = new List<int>();
                //string NewList;
                CurrentKeyFrame = 1;
                ChangeBy = 0;
                ChangeAll = false;
                Console.WriteLine($"Please enter what you'd like to swap these frame timings to.");
                Console.WriteLine($"Enter one number at a time, in the order they appeared in before.");
                Console.WriteLine($"If you enter nothing, the keyframe's original value will be used.");
                Console.WriteLine($"If you enter 'All', 'all', or 'a', you will be prompted to enter another value.");
                Console.WriteLine($"The remaining keyframes will then be computed by adding this value to their original value.");

                //This is the part that allows the user to make a list of their own keyframes
                foreach (int KeyFrame in UniqueKeyFrames)
                {
                    try
                    {
                        if (ChangeAll == true)
                        {
                            //NewKeyFramesList.Clear();
                            foreach (int i in UniqueKeyFrames)
                            {
                                //Allows it to skip any already set keyframes
                                if (UniqueKeyFrames.IndexOf(i) < CurrentKeyFrame - 1)
                                {
                                    continue;
                                }
                                //We always want to keep the first keyframe value equal to 1.
                                //This is because its both how all the other animations work,
                                //and because this program operates based on finding that first keyframe of 1
                                if (i == 1)
                                {
                                    NewKeyFramesList.Add(1);
                                }
                                else
                                {
                                    if ((i + ChangeBy) < 1)
                                    {
                                        Console.WriteLine($"One of the new computed keyframes is less than 1. Let's try again. {Environment.NewLine}");
                                        goto EpKeyFrameStuff;
                                    }
                                    NewKeyFramesList.Add(i + ChangeBy);
                                    //Console.WriteLine(ChangeBy);
                                }
                            }
                            break;
                        }
                        Console.WriteLine($"Enter keyframe {CurrentKeyFrame}");
                        Console.WriteLine($"This keyframe's old value was {UniqueKeyFrames.ElementAt(CurrentKeyFrame - 1)}");
                        if (UniqueKeyFrames.ElementAt(CurrentKeyFrame - 1) == 1 || (CurrentKeyFrame == 1))
                        {
                            Console.WriteLine($"It is highly recommended that you keep the first Keyframe set to 1");
                        }
                        string NewKeyFrame = Console.ReadLine();
                        if (String.IsNullOrWhiteSpace(NewKeyFrame))
                        {
                            KeyFrameForReplace = UniqueKeyFrames.ElementAt(CurrentKeyFrame - 1);
                            Console.WriteLine($"{KeyFrameForReplace}");
                            NewKeyFramesList.Add(KeyFrameForReplace);
                            ++CurrentKeyFrame;
                        }
                        else if (NewKeyFrame.Contains($"Restart") || NewKeyFrame.Contains($"restart"))
                        {
                            Epfs.Dispose();
                            goto Start;
                        }
                        else if (NewKeyFrame.Contains($"all") || NewKeyFrame.Contains($"All") || NewKeyFrame.Contains($"a"))
                        {
                            Console.WriteLine($"Enter what value to add to all of the remaining keyframes");
                            string ChangeInt = Console.ReadLine();
                            if (String.IsNullOrWhiteSpace(ChangeInt))
                            {
                                KeyFrameForReplace = UniqueKeyFrames.ElementAt(CurrentKeyFrame - 1);
                                Console.WriteLine($"{KeyFrameForReplace}");
                                NewKeyFramesList.Add(KeyFrameForReplace);
                                ++CurrentKeyFrame;
                                continue;
                            }
                            if (CurrentKeyFrame - 1 == 0)
                            {
                                NewKeyFramesList.Add(1);
                                ChangeBy = Convert.ToInt16(ChangeInt);
                                ++CurrentKeyFrame;
                                ChangeAll = true;
                            }
                            else
                            {
                                ChangeBy = Convert.ToInt16(ChangeInt);
                                KeyFrameForReplace = (UniqueKeyFrames.ElementAt(CurrentKeyFrame - 1) + ChangeBy);
                                NewKeyFramesList.Add(KeyFrameForReplace);
                                ++CurrentKeyFrame;
                                ChangeAll = true;
                            }
                            continue;
                        }
                        else
                        {
                            KeyFrameForReplace = Convert.ToInt16(NewKeyFrame);
                            NewKeyFramesList.Add(KeyFrameForReplace);
                            ++CurrentKeyFrame;
                        }
                    }

                    catch
                    {
                        Console.WriteLine($"Something went wrong, failed to read the entered keyframe");
                        Console.WriteLine($"Start from the top");
                        //NewKeyFramesList.Remove(CurrentKeyFrame);
                        goto EpKeyFrameStuff;
                    }
                }
                //int OldListLength = UniqueKeyFrames.Count;
                //int NewListLength = NewKeyFrames.Count;
                if (UniqueKeyFrames.Count - NewKeyFramesList.Count != 0)
                {
                    Console.WriteLine($"Error! List lengths do not match!");
                    Console.WriteLine($"Original List:{UniqueKeyFrames.Count}, New List:{NewKeyFramesList.Count}");
                    return;
                }

                Console.WriteLine($"Your new keyframes are:");
                NewKeyFramesList.ForEach(i => Console.WriteLine(i + ","));
                Console.WriteLine($"Are these values satisfactory? Type 'n' and hit enter if they are not. If they are satisfactory, just hit enter.");
                Satisfaction = Console.ReadLine();
                if (Satisfaction == "n")
                {
                    Console.WriteLine($"No? How about we try again then. {Environment.NewLine}");
                    goto EpKeyFrameStuff;
                }

                ConvertedBuffer = 0;
                CurrentKeyFrame = 0;


                foreach (int NewKeyFrameForReplace in NewKeyFramesList)
                {
                    try
                    {
                        //Resets the position for each int we search for
                        //Resets the buffer so it doesn't get stuck
                        Epfs.Position = FloatStartPos;
                        ConvertedBuffer = 1;
                        while ((ConvertedBuffer % 1) < float.Epsilon)
                        {
                            //Reads a float and stores it
                            Epfs.ReadExactly(FloatBuffer, 0, 4);
                            ConvertedBuffer = BitConverter.ToSingle(FloatBuffer, 0);

                            //Gets the float equivalent of a certain int in each list
                            //Need to rename these
                            var piss = UniqueKeyFrames[CurrentKeyFrame];
                            float pissfloat = Convert.ToSingle(piss);
                            byte[] pissbyte = BitConverter.GetBytes(pissfloat);
                            var shit = NewKeyFramesList[CurrentKeyFrame];
                            float shitfloat = Convert.ToSingle(shit);
                            byte[] shitbyte = BitConverter.GetBytes(shitfloat);
                            //Console.WriteLine(piss);
                
                            //Hits if the float matches one of the original keyframes
                            if (ConvertedBuffer == pissfloat)
                            {
                                //string test = BitConverter.ToString(pissbyte);
                                //Console.WriteLine(test);

                                Epfs.Position = (Epfs.Position - 4);


                                //Checks if this location has already been written to, and skips writing if it was.
                                //If this location hasn't been written to, it gets added to a list of locations- 
                                //-that have been written to, and then the bytes are overwritten.
                                if (AlreadyWrittenTo.Contains(Convert.ToInt32(Epfs.Position)))
                                {
                                    Epfs.Position += 4;
                                    continue;
                                }
                                else
                                {
                                    AlreadyWrittenTo.Add(Convert.ToInt32(Epfs.Position));
                                }

                                //Overwrites the float with the user entered float from the new list
                                Epfs.Write(shitbyte, 0, 4);
                    
                                //debug
                                string shitstring = Convert.ToHexString(shitbyte);
                                //Console.WriteLine(shitstring);

                                //UniqueKeyFrames.ForEach(i => {Console.WriteLine(i.ToString());});

                            }
                            //Should only hit when the stream goes past the floats
                            //This can be updated with more accurate code due to the new findings in the "header" part
                            if (ConvertedBuffer > 600 || ConvertedBuffer < 0 || ConvertedBuffer % 1 != 0)
                            {
                                //Console.WriteLine($"Parsed value too big!!!");
                                ++CurrentKeyFrame;
                                break;
                                //I'm so stupid
                                //ConvertedBuffer = 1;
                            }
                            //debug
                            //Console.WriteLine($"loop");
                        }
            
                    }
                    catch (Exception e)
                    {
                        //debug
                        Console.WriteLine(e.ToString());
                        Console.WriteLine($"shit");
                    }

        

                }
                SetEpEndFrame:
                Console.WriteLine($"Please enter what frame the animation ends on");
                Console.WriteLine($"The animation previously ended on frame {MoveLength}");
                Console.WriteLine($"(I recommend just making it the highest value you entered, which was {NewKeyFramesList.Max()})");
                EndingFrameString = Console.ReadLine();
                if (String.IsNullOrWhiteSpace(EndingFrameString))
                {
                    EndingFrameString = NewKeyFramesList.Max().ToString();
                    Console.WriteLine($"{EndingFrameString}");
                    //Console.WriteLine($"Null value entered, try entering your value again.");
                    //goto SetEpEndFrame;
                }
                EndingFrame = Convert.ToInt16(EndingFrameString);
                Epfs.Position = FinalOffset;
                EndingFrameBytes = new byte[2];
                EndingFrameBytes = BitConverter.GetBytes(EndingFrame);
   
                //Commented-out debug code.
                //string EndF = Convert.ToHexString(EndingFrameBytes);
                //Console.WriteLine(EndF);
    
                Epfs.Write(EndingFrameBytes, 0, 2);
                
                
                Epfs.Dispose();
                goto Code1Handling;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }


        }
        catch (Exception e) 
        { 
            Console.WriteLine(e.ToString());
        
        }

       
    }
    
}
    