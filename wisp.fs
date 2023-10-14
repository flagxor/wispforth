#! /usr/bin/env gforth

: ,2 ( n -- ) dup c, 8 rshift c, ;
: ,4 ( n -- ) dup ,2 16 rshift ,2 ;
: ,8 ( n -- ) dup ,4 32 rshift ,4 ;
: 0,   0 c, ;

$400000 $40 + $38 + constant entry-addr

variable image
variable fh

: start-image   here image ! ;
: end-image
  s" out/wisp" w/o bin create-file throw fh !
  image @ here image @ - fh @ write-file throw
  fh @ close-file throw
  s" chmod a+x out/wisp" system
;

: hash ( a n -- n )
  71 >r
  begin dup while
    swap dup c@ >r 1+ swap 1- r> r> 31 * + >r
  repeat
  2drop r>
;

: >number ( a n -- n )
  0 >r
  begin dup while
    swap dup c@ [char] 0 - r> base @ * + >r
    1+ swap 1-
  repeat
  2drop r>
;

: find ( a n dct -- xt )
  >r hash r>
  begin dup @ while
    2dup @ = if cell+ @ nip exit then
    2 cells -
  repeat
;

variable regular
variable macros
variable current
: +word ( nm adr -- )
  2 cells current @ +!
  current @ @ cell+ !
  current @ @ !
;

create dict
here regular !
0 , 0 ,
100 cells allot
here macros !
0 , 0 ,
100 cells allot
regular current !

: header   bl parse hash here +word ;

: ~]
  begin
    bl parse 2dup
    macros @ find if
      >r 2drop r> execute
    else
      regular @ find if
        >r 2drop r> $e8 here 1+ - ,4
      else
        >number
      then
    then
  again
;

: ~:   header ] ;


: postpone ;  ( do nothing in host )

: rex.W   $48 c, ;
: ++rsp   rex.W $83 c, $c4 c, $08 c, ( add $0x8,%rsp ) ;
: rsp+! ( n -- ) rex.W $81 c, $c4 c, ,4 ( add $n32,%rsp ) ;

variable offset
: rbp+= ( n -- ) dup negate offset +!
                 rex.W $83 c, $c5 c, c, ( add $n,%rbp ) ;
: rbp-= ( n -- ) dup offset +!
                 rex.W $83 c, $ed c, c, ( sub $n,%rbp ) ;
: offset@ ( -- n ) offset @ ;
: offset@- ( -- n ) offset @ 8 - ;
: balance   offset@ 0< if offset@ negate rbp-= then
            offset@ 0> if offset@ rbp+= then ;
: ++rbp   8 offset +! offset@ 120 >= if balance then ;
: --rbp   -8 offset +! offset@ -120 <= if balance then ;

: drop0   $31 c, $db c, ( xor %ebx,%ebx ) ;
: cmp0   rex.W $83 c, $fb c, $00 c, ( cmp $0x0,%rbx ) ;
: past>tos   rex.W $8b c, $5d c, $08 c, ( mov 0x8[%rbp],%rbx ) ;

: nip ( a b -- b ) --rbp ;
: dup' ( n -- n n ) ++rbp rex.W $89 c, $5d c, offset@ c, ( mov %rbx,o+0x0[%rbp] ) ;

: aliteral32u ( n -- ) dup' $bb c, ,4 ( mov $n32,%ebx ) ;
: aliteral32s ( n -- ) dup' rex.W $c7 c, $c3 c, ,4 ( mov $n32, %rbx ) ;
: aliteral64 ( n -- ) dup' rex.W $bb c, ,8 ( movabs $n64,%rbx ) ;
: aliteral ( n -- ) dup dup $ffffffff and = if
                      aliteral32u
                    else
                      dup negate $80000000 < if
                        aliteral32s
                      else
                        aliteral64
                      then
                    then ;

: begin   balance here ;
: again   balance $eb c, here 1+ - c, ;
: ahead   balance $eb c, here 0 c, ;
: then   balance here over 1+ - swap c! ;
: until ( n -- ) nip balance cmp0 past>tos $74 c, here 1+ - c, ;
: if ( n -- ) nip balance cmp0 past>tos $74 c, here 0 c, ;

: 1+ ( n -- n ) rex.W $ff c, $c3 c, ( inc %rbx ) ;
: 1- ( n -- n ) rex.W $ff c, $cb c, ( dec %rbx ) ;

: drop ( n -- ) rex.W $8b c, $5d c, offset@ c, ( mov o+0x0[%rbp],%rbx )
                postpone nip ;
: dup ( n -- n n ) dup' ;
: over ( n -- n n ) ++rbp rex.W $89 c, $5d c, offset@- c, ( mov %rbx,-0x8[%rbp] ) ;

: rdrop ( a b -- b ) rex.W $83 c, $ec c, $08 c, ( sub $0x8,%rsp ) ;
: push ( n -- r: n ) ++rsp rex.W $89 c, $1c c, $24 c, ( mov %rbx,[%rsp] )
                     postpone drop ;
: pop ( r: n -- n ) postpone dup
                    rex.W $8b c, $1c c, $24 c, ( mov [%rsp],%rbx )
                    postpone rdrop ;

: + ( n n -- n ) rex.W $03 c, $5d c, offset@ c, ( add o+0x0[%rbp],%rbx )
                 postpone nip ;
: - ( n n -- n ) rex.W $2b c, $5d c, offset@ c, ( sub o+0x0[%rbp],%rbx )
                 postpone nip ;
: * ( n n -- n ) rex.W $0f c, $af c, $5d c, offset@ c, ( imul o+0x0[%rbp],%rbx )
                 postpone nip ;

: and ( n n -- n ) rex.W $23 c, $5d c, offset@ c, ( and o+0x0[%rbp],%rbx )
                   postpone nip ;
: or ( n n -- n ) rex.W $0b c, $5d c, offset@ c, ( or o+0x0[%rbp],%rbx )
                  postpone nip ;
: xor ( n n -- n ) rex.W $33 c, $5d c, offset@ c, ( xor o+0x0[%rbp],%rbx )
                   postpone nip ;
: invert ( n -- n ) rex.W $f7 c, $d3 c, ( not %rbx ) ;
: negate ( n -- n ) rex.W $f7 c, $db c, ( neg %rbx ) ;

: 0= ( n -- n ) cmp0 drop0 $0f c, $9c c, $c3 c, ( setl %bl )
                postpone negate ;
: 0< ( n -- n ) cmp0 drop0 $0f c, $94 c, $c3 c, ( sete %bl )
                postpone negate ;

: exit    balance $c3 c, ;
: nop    $90 c, ;

: @ ( a -- n ) rex.W $8b c, $1b c, ( mov [%rbx],%rbx ) ;
: ! ( n a -- ) rex.W $8b c, $4d c, offset@ c, ( mov o+0x0[%rbp],%rcx )
               rex.W $89 c, $0b c, ( mov %rcx,[%rbx] )
               postpone nip postpone drop ;
: c@ ( a -- ch ) rex.W $0f c, $b6 c, $1b c, ( movzbq [%rbx],%rbx )
: c! ( ch a -- ) $8a c, $4d c, offset@ c, ( mov o+0x0[%rbp],%cl )
                 $88 c, $0b c, ( mov %cl,[%rbx] ) ;

: syscall ( n n n n n n - n )
   balance
   rex.W $89 c, $d8 c, ( mov %rbx,%rax )
   $4c c, $8b c, $4d c, $00 c, ( mov 0x0[%rbp],%r9 )
   $4c c, $8b c, $45 c, $F8 c, ( mov -0x8[%rbp],%r8 )
   $4c c, $8b c, $55 c, $F0 c, ( mov -0x10[%rbp],%r10 )
   rex.W $8b c, $55 c, $E8 c, ( mov -0x18[%rbp],%rdx )
   rex.W $8b c, $75 c, $E0 c, ( mov -0x20[%rbp],%rsi )
   rex.W $8b c, $7d c, $D8 c, ( mov -0x28[%rbp],%rdi )
   rex.W $83 c, $ed c, $30 c, ( sub $0x30,%rbp )
   $0f c, 05 c, ( syscall )
   rex.W $89 c, $c3 c, ( mov %rax,%rbx )
;

: init    rex.W $89 c, $e5 c, ( mov %rsp,%rbp ) $1000 rsp+! ;

: elf-magic
  $7f c, [char] E c, [char] L c, [char] F c,
  2 c, ( ELFCLASS64)
  1 c, ( ELFDATA2LSB )
  1 c, ( EV_CURRENT )
  3 c, ( ELFOSABI_LINUX )
  0, ( ABI version )
  0, 0, 0, 0, 0, 0, 0, ( EI_PAD )
;

: elf-header
  elf-magic
  2 ,2 ( e_type = ET_EXEC )
  62 ,2 ( e_machine = EM_X86_64 )
  1 ,4 ( e_version = EV_CURRENT )
  entry-addr ,8 ( e_entry = offset to entry below )
  $40 ,8 ( e_phoff = offset to program header below )
  0 ,8 ( e_shoff, no section header )
  0 ,4 ( e_flags )
  $40 ,2 ( e_ehdrsize = size of main header )
  $38 ,2 ( e_phentsize = header size below )
  1 ,2 ( e_phnum = 1 entry below )
  0 ,2 ( e_shentsize )
  0 ,2 ( e_shnum )
  0 ,2 ( e_shstrndx )
;

: program-header
  1 ,4 ( p_type = PT_LOAD )
  7 ,4 ( p_flags = PF_X | PF_W | PF_R )
  0 ,8 ( p_offset )
  $400000 ,8 ( p_vaddr )
  $400000 ,8 ( p_paddr )
  $100000 ,8 ( p_filesz )
  $100000 ,8 ( p_memsz )
  0 ,8 ( p_align )
;

start-image
elf-header
program-header
( START )
init
1 aliteral $400001 aliteral 3 aliteral 0 aliteral 0 aliteral 0 aliteral 1 aliteral syscall drop
42 aliteral 0 aliteral 0 aliteral 0 aliteral 0 aliteral 0 aliteral 60 aliteral syscall drop
end-image
bye
