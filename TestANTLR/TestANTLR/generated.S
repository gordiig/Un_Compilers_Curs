	.section	.data

	.section	.text

func_main_start:
	// Prologue
	allocframe(#4000)
	// End of prologue
	// Variable a declaration
	memb(SP + #0) = #0;
	// Setting value for variable a
	// Value for unary operation:
	// Getting constant 66
	r0 = #66;
	// Unary -
	r1 = neg(r0);
	r0 = add(SP, #0)
	memb(r0) = r1;
	// Variable b declaration
	memb(SP + #1) = #0;
	// Setting value for variable b
	// Getting variable a address
	r0 = add(SP, #0)
	r1 = memb(r0);
	r0 = add(SP, #1)
	memb(r0) = r1;
	// Getting constant 1
	r0 = #1;
	// Getting variable "a" for lvalue
	r1 = add(SP, #0)
	r2 = memb(r1);
	// Assigning with some assign operator
	r2 = add(r2, r0);
	memw(r1) = r2;
	// Getting variable a address
	r0 = add(SP, #0)
	r1 = memb(r0);
	// Getting variable "b" for lvalue
	r0 = add(SP, #1)
	r2 = memb(r0);
	// Assigning with some assign operator
	r2 = r1;
	memb(r0) = r2;
func_main_end:
	dealloc_return;

