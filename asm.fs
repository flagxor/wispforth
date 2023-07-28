: registers ( start n -- ) 0 do dup constant 1+ loop drop ;

$00 16 registers al cl dl bl ah ch dh bh r8l r9l r10l r11l r12l r13l r14l r15l
$10 16 registers ax cx dx bx sp bp si di r8w r9w r10w r11w r12w r13w r14w r15w
$20 16 registers eax ecx edx ebx esp ebp esi edi r8d r9d r10d r11d r12d r13d r14d r15d 
$30 16 registers rax rcx rdx rbx rsp rbp rsi rdi r8 r9 r10 r11 r12 r13 r14 r15

: regext? ( r -- f ) 8 and 0<> ;
: 64-bit? ( r -- f ) $f0 and $30 = ;
: rex ( wr x b -- ) regext? 1 and
                    swap regext? 2 and or
                    over regext? 4 and or
                    swap 64-bit? 8 and or
                    dup if $40 or c, else drop then ;

: push, ( r -- ) dup >r 0 0 r> rex $7 and $50 or c, ;
: pop, ( r -- ) dup >r 0 0 r> rex $7 and $58 or c, ;


