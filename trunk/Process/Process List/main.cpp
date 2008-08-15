#include <iostream>

#include "Process.h"

using namespace std;

int main()
{
    Process notepad = Process::GetProcessesByName( L"notepad.exe" )[0];
    
    notepad[ 0x01009020 ].Write( "T" );
    
    wcout << notepad[ 0x01009020 ].ReadUnicode( 8 );
    
    cout << endl;
    system("pause");
    return 0;
}
