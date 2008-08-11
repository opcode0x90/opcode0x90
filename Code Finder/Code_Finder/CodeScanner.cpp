/*
 * codescanner.h - scanning for code segment using specified signature
 */

#include "CodeScanner.h"

/*
 * public functions
 */
unsigned int CodeScanner::Scan( void *buffer, unsigned int len, std::string sig )
{
    // pass to char* version of this scanner
    return CodeScanner::Scan( buffer, len, (char*)sig.c_str(), sig.size() );
}

unsigned int CodeScanner::Scan( void *buffer, unsigned int len, char* sig, unsigned int siglen )
{
    unsigned int beginaddr = (unsigned int)buffer;
    unsigned int endaddr = ( beginaddr + len - siglen );
    
    // scan for the specified code segment
    for ( unsigned int i = beginaddr; i <= endaddr; i++ )
    {
        // is this what we wanted ?
        if ( strncmp( (char*)i, sig, siglen ) == 0 )
        {
            // match
            return i;
        }
        
        // debug
        printf("i: 0x%.8x\n", i);
    }


    // invalid argument
    return 0;
}
