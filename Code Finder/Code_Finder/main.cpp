#include <iostream>
#include "CodeScanner.h"

using namespace std;

int main()
{
    cout << "Hello, world" << endl;
    
    // give it a test drive
    printf( "Result: 0x%.8x\n", CodeScanner::Scan( (void*)0x00410000, 0x00010000, "Hello, world" ) );
    
    system("pause");
    return 0;
}
