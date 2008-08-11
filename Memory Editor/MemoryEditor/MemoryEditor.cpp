// MemoryEditor.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

#include <iostream>
#include "memory.h"

using namespace std;

int _tmain(int argc, _TCHAR* argv[])
{
    // give the memory editor a try
    Memory mem( 3524 );
    
    cout << "mem.Read: " << mem.Read( 0xA9A14, 14 ) << endl;
    cout << "mem.Content: " << *mem.Content << endl;
    
    system("pause");
	return 0;
}
