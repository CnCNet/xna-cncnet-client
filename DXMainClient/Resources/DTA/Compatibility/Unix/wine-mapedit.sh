#!/bin/sh
wineconsole Resources/Compatibility/Unix/wine-mapedit.bat &
BACK_PID=$!
wait $BACK_PID
