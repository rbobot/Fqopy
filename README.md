Fqopy
====

A _Fast_ & _Quick_ Powershell File Copy Module with CRC Check.

XCopy, Robocopy and Teracopy are all good Windows utilities for copying a large amount of files, but none of them offer easily machine-readable results report. An efficient, reliable and audit-able _copy utility_ is critical for large-scale, automated software deployments.

Fqopy is a binary Powershell Module that provides some the functionality of all of those other utilities with an object output for advanced reporting and auditing. Fqopy copies only the data inside each file and none of the metadata like ModifiedTime or ACLs.

Two Cmdlets are exported.

	NAME
		Copy-Files

	SYNOPSIS
		A Powershell File Copy Module with CRC Check.

	SYNTAX
		Copy-Files [-Source] <string> [-Destination] <string> [[-Filter] <string>]
		[[-Recurse]] [[-Fast]] [[-Overwrite]] [[-ShowProgress]] [[-List]] [[-PassThru]] [<CommonParameters>]


	NAME
		Get-CopyResultsReport

	SYNOPSIS
		Aggregate output of Copy-Files

	SYNTAX
		Get-CopyResultsReport -InputObject <FileCopyResultsItem>
		[<CommonParameters>]		

The output is an object list with the following properties&#151;One object per source file.

	   TypeName: Fqopy.FileCopyResultsItem

	Name           MemberType Definition
	----           ---------- ----------
	Destination    Property   string Destination {get;set;}
	DestinationCRC Property   string DestinationCRC {get;set;}
	ErrorMessage   Property   string ErrorMessage {get;set;}
	Match          Property   bool Match {get;set;}
	Size           Property   long Size {get;set;}
	Source         Property   string Source {get;set;}
	SourceCRC      Property   string SourceCRC {get;set;}
	Time           Property   timespan Time {get;set;}

Get-CopyResultsReport returns the following properties.

    TypeName: Fqopy.FileCopyResultsReport

	Name           MemberType Definition
	----           ---------- ----------
	Bytes          Property   long Bytes {get;set;}
	FailedItemList Property   System.Collections.Generic.List[Fqopy.FileCopyResultsItem]
	FileCount      Property   int FileCount {get;set;}
	TotalTime      Property   timespan TotalTime {get;set;}	
Features
====
* Attempts to not copy files when unnecessary.
    * If file size is different and `Overwrite` switch is not enabled, do not copy.
    * If file size is the same and CRC check is the same, do not copy.
* Validates source and destination contents match

CRC-32 functionality was developed by Damien Guard - [Calculating CRC-32 in C# and .NET](http://damieng.com/blog/2006/08/08/calculating_crc32_in_c_and_net)

Performance
====
Test Case:
1,797 Files in 145 Folders
108MB

_Local System_<br />
**TeraCopy w/Test** 1:01<br />
**Fqopy** 0:27<br />

_LAN_<br />
**TeraCopy w/Test** 2:02<br />
**Fqopy** 1:31<br />

Changes
====

1.0.3
----
* Added the Fast switch to prevent copy timestamp metadata. This increases copy time by as much as 300%.
* Added PassThru switch
* Added List parameter to copying files from text file with relastive paths:
	Copy-Files C:\users\downloads D:\filearchive -Recurse -List C:\users\downloads\queue.txt

1.0.2
----
* Changed error output to use WriteVerbose. This prevents terminating errors. Add the -Verbose switch to expose.
* Added ErrorMessage property to the FileCopyResultsItem class to capture copy errors.

1.0.1
----
* Removed some unnecessary IO by eliminating redundant file existence checks. Should improve network file copy performance a little bit.


Todo
====
* A bunch of code clean-up
* Update Progress bar to use Bytes instead of File Count
* Move (delete source)?
* Validate only?
* Do not Validate?

Use
====
**Requires Powershell v2.**

[Binary Download](https://github.com/rbobot/Fqopy/releases/)

1. Extract to `C:\Users\[username]\Documents\WindowsPowerShell\Modules` 
1.  `Import-Module Fqopy`

If you use the `ShowProgress` switch without capturing the Output, the screen will get very flashy.

