@ECHO OFF
ECHO Enter credentials
SET /P user_pass=
ECHO Reporting connection mode
ECHO Enter the Collection URI
SET /P collectionUri=
ECHO AnnotationFailedOnProject
>&2 ECHO Exception on Annotating Project
