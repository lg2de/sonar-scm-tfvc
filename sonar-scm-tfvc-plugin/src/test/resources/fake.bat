@ECHO OFF
ECHO Enter credentials
SET /P user_pass=
ECHO Enter paths to annotate
SET /P p=
ECHO %p%
TYPE %p:/=\%
