@ECHO OFF
ECHO Enter credentials
SET /P user_pass=
ECHO Enter the Collection URI
SET /P collectionUri=
ECHO Enter paths to annotate
SET /P p=
ECHO %p%
TYPE %p:/=\%
