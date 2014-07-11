## SonarQube TFS annotate command line tool

### How to use?

	SonarTfsAnnotate.exe [filename]

which, for example, outputs:

	26274 SND\DinSoft_cp 07/10/2014 hello,
	26274 SND\DinSoft_cp 07/10/2014 world!
	local I need to check this line in.
	26275 SND\DinSoft_cp 07/13/2014 woohoo!

the format is, depending on the state of the line:

* committed: [changeset id] [committer] [MM/dd/yyyy] [line contents]
* local: local [line contents]
* unknown: unknown [line contents]

### Features

* Reports the last changeset id, committer and date next to each line
* Up to 4 times faster than "tfpt.exe annotate", by prefetching older revisions
* Supports branches, the committer, not the owner, is displayed
* Supports TFS 2013 (not yet tested with older versions)
* Supports local modifications
* Supports local versions different from latest
* Supports changes of file encoding, including from and to binary
* Requires the .NET framework 4.5 to be installed
* Compatible with the released SCM Activity plugin 1.7.1

### Issue tracker

http://jira.codehaus.org/browse/SONARTFS
