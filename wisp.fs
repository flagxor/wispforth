#! /usr/bin/env gforth

: ,2 ( n -- ) dup c, 8 rshift c, ;
: ,4 ( n -- ) dup ,2 16 rshift ,2 ;
: ,8 ( n -- ) dup ,4 32 rshift ,4 ;
: 0,   0 c, ;

variable image
variable fh

: start-image   here image ! ;
: end-image
  s" out/wisp" w/o bin create-file throw fh !
  image @ here image @ - fh @ write-file throw
  fh @ close-file throw
  s" chmod a+x out/wisp" system
;
: o+ ( n n -- n ) + ;

: rex.W   $48 c, ;
: ++rbp   rex.W $83 c, $c5 c, $08 c, ( add $0x8,%rbp ) ;
: ++rsp   rex.W $83 c, $c4 c, $08 c, ( add $0x8,%rsp ) ;

: 1+ ( n -- n ) rex.W $ff c, $c3 c, ( inc %rbx ) ;
: 1- ( n -- n ) rex.W $ff c, $cb c, ( dec %rbx ) ;
: rdrop ( a b -- b ) rex.W $83 c, $ec c, $08 c, ( sub $0x8,%rsp ) ;
: nip ( a b -- b ) rex.W $83 c, $ed c, $08 c, ( sub $0x8,%rbp ) ;
: drop ( n -- ) rex.W $8b c, $5d c, $00 c, ( mov 0x0[%rbp],%rbx ) nip ;
: dup ( n -- n n ) ++rbp rex.W $89 c, $5d c, $00 c, ( mov %rbx,0x0[%rbp] ) ;
: over ( n -- n n ) ++rbp rex.W $89 c, $5d c, $f8 c, ( mov %rbx,-0x8[%rbp] ) ;
: push ( n -- r: n ) ++rsp rex.W $89 c, $1c c, $24 c, ( mov %rbx,[%rsp] ) drop ;
: pop ( r: n -- n ) dup rex.W $8b c, $1c c, $24 c, ( mov [%rsp],%rbx ) rdrop ;
: + ( n n -- n ) rex.W $03 c, $5d c, $00 c, ( add 0x0[%rbp],%rbx ) nip ;
: - ( n n -- n ) rex.W $2b c, $5d c, $00 c, ( sub 0x0[%rbp],%rbx ) nip ;
: * ( n n -- n ) rex.W $0f c, $af c, $5d c, $00 c, ( imul 0x0[%rbp],%rbx ) nip ;
: and ( n n -- n ) rex.W $23 c, $5d c, $00 c, ( and 0x0[%rbp],%rbx ) nip ;
: or ( n n -- n ) rex.W $0b c, $5d c, $00 c, ( or 0x0[%rbp],%rbx ) nip ;
: xor ( n n -- n ) rex.W $33 c, $5d c, $00 c, ( xor 0x0[%rbp],%rbx ) nip ;

start-image

( ELF HEADER )
$7f c, char E c, char L c, char F c,
2 c, ( ELFCLASS64)
1 c, ( ELFDATA2LSB )
1 c, ( EV_CURRENT )
3 c, ( ELFOSABI_LINUX )
0, ( ABI version )
0, 0, 0, 0, 0, 0, 0, ( EI_PAD )

2 ,2 ( e_type = ET_EXEC )
62 ,2 ( e_machine = EM_X86_64 )
1 ,4 ( e_version = EV_CURRENT )
$400000 $40 $38 o+ o+ ,8 ( e_entry = offset to entry below )
$40 ,8 ( e_phoff = offset to program header below )
0 ,8 ( e_shoff, no section header )
0 ,4 ( e_flags )
$40 ,2 ( e_ehdrsize = size of main header )
$38 ,2 ( e_phentsize = header size below )
1 ,2 ( e_phnum = 1 entry below )
0 ,2 ( e_shentsize )
0 ,2 ( e_shnum )
0 ,2 ( e_shstrndx )

( PROGRAM HEADER )
1 ,4 ( p_type = PT_LOAD )
7 ,4 ( p_flags = PF_X | PF_W | PF_R )
0 ,8 ( p_offset )
$400000 ,8 ( p_vaddr )
$400000 ,8 ( p_paddr )
$100000 ,8 ( p_filesz )
$100000 ,8 ( p_memsz )
0 ,8 ( p_align )

( START ) 
( 401000: ) $b8 c, $3c c, $00 c, $00 c, $00 c, ( mov    $0x3c,%eax )
( 401005: ) $bf c, $2a c, $00 c, $00 c, $00 c, ( mov    $0x2a,%edi )
( 40100a: ) $0f c, $05 c,                      ( syscall )

dup *

end-image
bye
