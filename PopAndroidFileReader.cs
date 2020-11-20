using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PopAndroidFileReader
{
	long FileOffset;
	long FileLength = 0;
	long FileCurrentPosition = 0;
	AndroidJavaObject FileDescriptor;
	AndroidJavaObject AssetFileDescriptor;
	AndroidJavaObject FileInputStream;

	public PopAndroidFileReader(string InternalFilename, string JarOrZipOrApkFilename = null)
	{
		//	default to app's apk
		//	 "jar: file://" + Application.dataPath + "!/assets/" + Filename;
		if (JarOrZipOrApkFilename == null || JarOrZipOrApkFilename.Length == 0)
			JarOrZipOrApkFilename = Application.dataPath;

		//	this is the prefix in APK's, but this code shouldn't assume it's always here, we could just be reading any zip file
		InternalFilename = "assets/" + InternalFilename;

		AndroidJavaObject ZipFile;
		try
		{
			//	"com.android.vending.expansion.zipfile.ZipResourceFile"
			//	"com.google.android.vending.expansion.zipfile"
			var PackageName = "com.google.android.vending.expansion.zipfile";   //	package name inside the .java
			var ClassName = "ZipResourceFile";  //	class inside .java
			ZipFile = new AndroidJavaObject(PackageName + "." + ClassName, JarOrZipOrApkFilename);
		}
		catch (AndroidJavaException e)
		{
			throw new System.Exception("Failed to open zip file (" + JarOrZipOrApkFilename + "): " + e.Message);
		}
		/*
		var StringSig = "Ljava/lang/String;";
		var VoidSig = "V";

		var ZipClass = AndroidJNI.FindClass("com.android.vending.expansion.zipfile.ZipResourceFile");
		var ConstructorName = "<init>";
		var ConstructorSignature = "("+StringSig+")" + VoidSig +"";    //	1 string arg, return void
		var ConstructorMethod = AndroidJNI.GetMethodID(ZipClass, ConstructorName, ConstructorSignature);
		var JarOrZipOrApkFilename_j = AndroidJNI.NewString(JarOrZipOrApkFilename);

		var ConstructorArguments = new jvalue[1];
		ConstructorArguments[0].l = JarOrZipOrApkFilename_j;	//	.l = L in signature
		ZipFile = AndroidJNI.NewObject(ZipClass, ConstructorMethod, ConstructorArguments);
		if (ZipFile==System.IntPtr.Zero)
		*/
		if (ZipFile == null)
			throw new System.Exception("Failed to open zip/jar/apk " + JarOrZipOrApkFilename);

		var GetAssetFileDescriptorName = "getAssetFileDescriptor";
		try
		{
			AssetFileDescriptor = ZipFile.Call<AndroidJavaObject>(GetAssetFileDescriptorName, InternalFilename);
			/*
			var AssetFileDescriptorClassName = "android/content/res/AssetFileDescriptor";
			var AssetFileDescriptorSig = "L" + AssetFileDescriptorClassName + ";";
			var getAssetFileDescriptorSignature = "(" + StringSig + ")" + AssetFileDescriptorSig + "";
			var getAssetFileDescriptorMethod = AndroidJNI.GetMethodID(ZipClass, GetAssetFileDescriptorName, getAssetFileDescriptorSignature);
			var InternalFilename_j = AndroidJNI.NewString(InternalFilename);
			var GetAssetFileDescriptorArguments = new jvalue[1];
			GetAssetFileDescriptorArguments[0].l = InternalFilename_j;
			var FileDescriptor = AndroidJNI.CallObjectMethod(ZipFile, getAssetFileDescriptorMethod, GetAssetFileDescriptorArguments);
			//	gr: returns a null object when filenot found (no exception set in java vm!)
			if (FileDescriptor == System.IntPtr.Zero)
			*/
			if (AssetFileDescriptor == null)
				throw new System.Exception("Opened zip but failed to open " + InternalFilename);
		}
		catch (AndroidJavaException e)
		{
			throw new System.Exception("Failed to open file inside zip file (" + InternalFilename + "): " + e.Message);
		}

		try
		{
			FileOffset = AssetFileDescriptor.Call<long>("getStartOffset");
			FileLength = AssetFileDescriptor.Call<long>("getLength");

			/*	gr: i used this in my c++ code, but I can't find any documentation for it!
					and now the class has no descriptor
			var fd = AssetFileDescriptor.Get<int>("descriptor");		
			Debug.Log("fd=" + fd);
			*/
			var FileDescriptor = AssetFileDescriptor.Call<AndroidJavaObject>("getFileDescriptor");
			if (FileDescriptor == null)
				throw new System.Exception("FileDescriptor inside Zip's AssetFileDescriptor is null");

			/*	gr: this gets file descriptor (int fd), but we can't actually do anything with it
			var ParcelFileDescriptor = AssetFileDescriptor.Call<AndroidJavaObject>("getParcelFileDescriptor");
			if (ParcelFileDescriptor == null)
				throw new System.Exception("ParcelFileDescriptor inside Zip's AssetFileDescriptor is null");

			var fd = ParcelFileDescriptor.Call<int>("getFd");
			Debug.Log("fd=" + fd);
			*/
			//	a filestream we can use for arbritry reads
			FileInputStream = new AndroidJavaObject("java.io.FileInputStream", FileDescriptor);
			if (FileInputStream == null)
				throw new System.Exception("Failed to create FileInputStream from file descriptor");
		}
		catch (AndroidJavaException e)
		{
			throw new System.Exception("Failed to open file inside zip file (" + InternalFilename + "): " + e.Message);
		}

		//throw new System.Exception("Got file descriptor offset="+ FileOffset +" length="+ FileLength);
		Debug.Log("Got file descriptor, and file input stream; offset=" + FileOffset + " length=" + FileLength);
	}

	public void Close()
	{
		if (AssetFileDescriptor != null)
			AssetFileDescriptor.Call("close");
	}


	public int GetFileSize()
	{
		return (int)FileLength;
	}

	public byte[] ReadBytes(long Position, long Size)
	{
		var ReadPosition = Position + FileOffset;
		if (ReadPosition < FileCurrentPosition)
			throw new System.Exception("Trying to read " + ReadPosition + "(" + Position + " + offset " + FileOffset + ") but currently at " + FileCurrentPosition + " and cannot go backwards");

		//	move into place
		var ToSkip = ReadPosition - FileCurrentPosition;
		var Skipped = FileInputStream.Call<long>("skip", ToSkip);
		FileCurrentPosition += Skipped;
		if (Skipped < ToSkip)
			throw new System.Exception("Trying to skip " + ToSkip + " but only skipped " + Skipped + " Position is " + FileCurrentPosition + " FileOffset=" + FileOffset + " FileLength=" + FileLength + "");

		Size = Mathf.Min((int)Size, 200);

		//	sbyte reported as obsolete, "use sbyte"
		//	gr: sbyte always reads only 0 bytes
		var Bufferj = AndroidJNI.NewSByteArray((int)Size);
		//var Bufferj = AndroidJNI.NewByteArray((int)Size);
		//var Buffer = new sbyte[Size];
		//var BytesRead = FileInputStream.Call<int>("read", Buffer);
		var BytesRead = FileInputStream.Call<int>("read", Bufferj);
		FileCurrentPosition += BytesRead;
		Debug.Log("Read " + BytesRead + "/" + Size);

		var Buffer = AndroidJNI.FromSByteArray(Bufferj);

		for (var i = 0; i < BytesRead; i += 200)
		{
			Debug.Log("Data from " + i);
			string SubStr = "";
			for (var d = 0; d < 200; d++)
				SubStr += Buffer[i] + " ";
			Debug.Log(SubStr);
			break;
		}


		//	-1 means EOF
		if (BytesRead == -1)
			throw new System.Exception("End of File at " + FileCurrentPosition);

		if (BytesRead <= 0)
			return null;

		//	turn sbyte to byte
		var Bufferu = new byte[BytesRead];
		for (var i = 0; i < Bufferu.Length; i++)
		{
			Bufferu[i] = (byte)Buffer[i];
		}
		return Bufferu;

		/*
		//	gr: cannot use SubArray with this (array.copy fails)
		//var Bufferu = (byte[])(System.Array)Buffer;

		if (BytesRead < Buffer.Length)
		{
			Bufferu = Bufferu.SubArray(0, BytesRead);
		}
		return Bufferu;
		*/
	}
}
